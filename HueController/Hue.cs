using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models;
using Q42.HueApi.Models.Bridge;
using Q42.HueApi.Models.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HueController
{
    public class Hue
    {
        private const int ScentUpdateThreshold = 10;

        private TimeSpan lightTansitionTime;

        private TimeSpan currentSleepDuration;

        private DateTime currentWakeCycle;

        private Flux flux = null;

        private FluxTimers fluxTimers = null;

        private List<KeyValuePair<HueBridge, List<LightDetails>>> clients = null;

        private CancellationTokenSource cancellationTokenSource = null;

        /// <summary>
        /// Logging
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Singleton
        /// </summary>
        private static Hue myHue = null;
        private static Object thisLock = new Object();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Hue GetOrCreate()
        {
            lock (thisLock)
            {
                if (null == myHue)
                {
                    myHue = new Hue();
                }
            }

            return myHue;
        }

        /// <summary>
        /// Status retrival property.
        /// </summary>
        public HueDetails Status
        {
            get
            {
                return new HueDetails()
                {
                    FluxStatus = flux.Status,
                    On = cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested,
                    CurrentSleepDuration = currentSleepDuration,
                    CurrentWakeCycle = currentWakeCycle,
                };
            }
        }

        /// <summary>
        /// Start the Flux worker.
        /// </summary>
        public void Start()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                throw new ArgumentException($"'{nameof(Start)}' should not be invoked twice without first invoking '{nameof(Stop)}'.");
            }

            // Parse the config JSON on the fly
            SetFluxConfigValues();

            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => FluxUpdateThread(cancellationTokenSource.Token), cancellationTokenSource.Token);

            // Parse the timer JSON on the fly
            Dictionary<string, string> lightsToAdd = GetListOfLightsWithIds();
            this.fluxTimers = FluxTimers.Create(lightsToAdd);

            foreach (FluxRule rule in fluxTimers.Rules)
            {
                Task.Run(() => FluxTimerThread(rule, cancellationTokenSource.Token), cancellationTokenSource.Token);
            }
        }

        /// <summary>
        /// Discontinue Flux updates.
        /// </summary>
        public void Stop()
        {
            if (this.clients == null || this.clients.Count() == 0)
            {
                throw new ArgumentException($"Lights must be added with '{nameof(AddLights)}' prior to invoking '{nameof(Stop)}'.");
            }

            if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested)
            {
                throw new ArgumentException($"'{nameof(Stop)}' should only be invoked after first invoking '{nameof(Start)}'.");
            }

            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Instiantiates the Flux client.
        /// </summary>
        /// <param name="hueConfig"></param>
        private Hue()
        {
            Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void SetFluxConfigValues()
        {
            FluxConfig fluxConfig = FluxConfig.ParseConfig();

            lightTansitionTime = fluxConfig.LightTransitionDuration;

            if (clients == null)
            {
                clients = ConnectClient(fluxConfig.BridgeIds);

                AddLights(LightEntityRegistry.DeserializeLightObjectGraph());
            }

            if (flux == null)
            {
                flux = new Flux();
            }

            flux.Latitude = fluxConfig.Latitude;
            flux.Longitude = fluxConfig.Longitude;
            flux.StopTime = fluxConfig.StopTime;
            flux.SolarNoonTemperature = fluxConfig.SolarNoonTemperature;
            flux.StopColorTemperature = fluxConfig.StopColorTemperature;
            flux.SunriseColorTemperature = fluxConfig.SunriseColorTemperature;
            flux.SunsetColorTemperature = fluxConfig.SunsetColorTemperature;

            log.Info($"'{nameof(Start)}' Sunrise will occur at '{flux.Sunrise}' and color '{flux.SunriseColorTemperature}'.");
            log.Info($"'{nameof(Start)}' Noon will occur at'{flux.SolarNoon}' and color '{flux.SolarNoonTemperature}'.");
            log.Info($"'{nameof(Start)}' Sunset will occur at '{flux.Sunset}' and color '{flux.SunsetColorTemperature}'.");
            log.Info($"'{nameof(Start)}' StopDate will occur at '{flux.StopDate}' and color '{flux.StopColorTemperature}'.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetListOfLightsWithIds()
        {
            Dictionary<string, string> lightNamesToId = new Dictionary<string, string>();

            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in clients)
            {
                IEnumerable<Light> lights = client.Key.Client.GetLightsAsync().Result;

                foreach (Light light in lights)
                {
                    lightNamesToId.Add(light.Name, light.Id);
                }
            }

            return lightNamesToId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fluxRule"></param>
        /// <param name="cancellationToken"></param>
        private void FluxTimerThread(FluxRule fluxRule, CancellationToken cancellationToken)
        {
            // Infinite loop until told to stop is requested
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime whenToWake = DateTime.Today + fluxRule.Time;
                if (fluxRule.Sunrise)
                {
                    whenToWake = flux.Sunrise;
                }
                else if (fluxRule.Sunset)
                {
                    whenToWake = flux.Sunset;
                }

                //DEBUG
                //if (fluxRule.Name == "test")
                //{
                //    whenToWake = DateTime.Now + TimeSpan.FromSeconds(5);
                //}

                if (DateTime.Now > whenToWake)
                {
                    whenToWake = whenToWake.AddDays(1);
                }

                log.Info($"'{nameof(FluxTimerThread)}' triggering at '{whenToWake.ToShortTimeString()}' for '{fluxRule.Name}' with lights '{String.Join(", ", fluxRule.LightIds)}'.");

                Thread.Sleep(whenToWake - DateTime.Now);

                if (!cancellationToken.IsCancellationRequested)
                {
                    foreach (KeyValuePair<HueBridge, List<LightDetails>> client in clients)
                    {
                        if (client.Key.BridgeId == fluxRule.BridgeId)
                        {
                            List<string> lightIds = fluxRule.LightIds;

                            // When OnlyReactWithState is set, remove any lights which don't match requested state value
                            if (fluxRule.OnlyReactWithState != FluxRule.ReactWithState.Any)
                            {
                                List<Light> allLights = client.Key.Client.GetLightsAsync().Result.ToList();

                                foreach (Light light in allLights)
                                {
                                    if (lightIds.Any(x => x.Equals(light.Id, StringComparison.InvariantCultureIgnoreCase)))
                                    {
                                        if (light.State.On && fluxRule.OnlyReactWithState == FluxRule.ReactWithState.Off ||
                                            !light.State.On && fluxRule.OnlyReactWithState == FluxRule.ReactWithState.On)
                                        {
                                            lightIds.Remove(light.Id);
                                        }
                                    }
                                }
                            }

                            // Ensure there's at least one light to adjust
                            if (lightIds.Count() > 0)
                            {
                                LightCommand lightCommand = new LightCommand()
                                {
                                    On = (fluxRule.State == FluxRule.States.On),
                                    Brightness = fluxRule.Brightness,
                                    TransitionTime = fluxRule.TransitionDuration,
                                };

                                // Set the color temperature when requested
                                if (fluxRule.SetFluxColorTemperature)
                                {
                                    lightCommand.ColorTemperature = flux.GetColorTemperature(DateTime.Now);
                                }

                                HueResults result = client.Key.Client.SendCommandAsync(lightCommand, lightIds.ToArray()).Result;

                                log.Info($"'{nameof(FluxTimerThread)}' activated '{fluxRule.Name}' with brightness '{fluxRule.Brightness}' for lights '{String.Join(", ", lightIds)}'.");
                            }                            
                        }
                    }
                }
            }

            // We're no longer running so allow another thread to be kicked off later
            log.Info($"'{nameof(FluxTimerThread)}' for '{fluxRule.Name}' now terminating.");
        }

        /// <summary>
        /// Flux worker thread
        /// </summary>
        private void FluxUpdateThread(CancellationToken cancellationToken)
        {
            log.Info($"'{nameof(FluxUpdateThread)}' now running.");

            // Infinite loop until told to stop by master thread
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;

                // Get the color temperature for the given time of day
                int colorTemperature = flux.GetColorTemperature(now);

                //log.Info($"'{nameof(FluxUpdateThread)}' got color temperature to set of '{colorTemperature}'.");

                foreach (KeyValuePair<HueBridge, List<LightDetails>> client in clients)
                {
                    // Only process a hub if there are lights in that hub to check
                    if (client.Value.Count() > 0)
                    {
                        // Send light update commands to lights which are currently 'On'
                        ModifyFluxLights(client.Key.Client, client.Value, colorTemperature, lightTansitionTime);

                        // Update the underlying 'Flux' scenes
                        ModifyFluxSwitchScenes(client.Key.Client, client.Value, colorTemperature);
                    }
                }

                // Get our next sleep duration
                TimeSpan currentSleepDuration = flux.GetThreadSleepDuration(now);
                log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{colorTemperature}' complete; sleeping for '{currentSleepDuration.TotalMinutes}' minutes and will resume at '{now + currentSleepDuration}'.");

                // Set updated status prior to invoking callbacks
                this.currentSleepDuration = currentSleepDuration;
                this.currentWakeCycle = now + currentSleepDuration;

                // Wait for the next interval which will require an update
                Thread.Sleep(currentSleepDuration);
            }

            // We're no longer running so allow another thread to be kicked off later
            log.Info($"'{nameof(FluxUpdateThread)}' now terminating.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        private void ModifyFluxLights(HueClient client, List<LightDetails> lightsToScan, int colorTemperature, TimeSpan transitiontime)
        {
            // Lights to update
            List<string> lightsToUpdate = new List<string>();

            foreach (Light light in client.GetLightsAsync().Result)
            {
                if (lightsToScan.Any(a => a.Id == light.Id))
                {
                    // Flux colot temperature adjustment
                    if (light.State.On && 
                        light.State.ColorMode != "hs" && // only operate on bulbs when in ambiance-lighting mode
                        light.State.ColorTemperature != colorTemperature)
                    {
                        // For white ambiance lights, don't adjust the color temperature when over the allowed threshold
                        if (LightDetails.IsInAllowedColorRange(lightsToScan.First(a => a.Id == light.Id).Type, colorTemperature))
                        {
                            lightsToUpdate.Add(light.Id);
                            //log.Debug($"'{nameof(ModifyFluxLights)}' adding '{light.Id}', '{light.Name}' to the modify list.");
                        }
                    }
                }
            }

            // Send the light update command
            if (lightsToUpdate.Count() > 0)
            {
                try
                {
                    LightCommand lightCommand = new LightCommand()
                    {
                        //On = true,
                        ColorTemperature = colorTemperature,
                        TransitionTime = transitiontime,
                    };

                    HueResults result = client.SendCommandAsync(lightCommand, lightsToUpdate).Result;

                   // log.Info($"    '{nameof(ModifyFluxLights)}' set '{lightsToUpdate.Count()}' lights to color temperature '{colorTemperature}' to '{String.Join(", ", lightsToUpdate)}'.");
                }
                catch (Exception)
                {
                    log.Error($"Exception: '{nameof(ModifyFluxLights)}' sent update request for ColorTemperature '{colorTemperature}' to '{String.Join(", ", lightsToUpdate)}'.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToSet"></param>
        /// <param name="colorTemperature"></param>
        private void ModifyFluxSwitchScenes(HueClient client, List<LightDetails> lightsToSet, int colorTemperature)
        {
            // Scenes to update
            foreach (Scene sceneId in client.GetScenesAsync().Result)
            {
                // Only modify 'Flux' scenes
                if (sceneId.Name.ToLowerInvariant().Contains("flux"))
                {
                    Scene scene = client.GetSceneAsync(sceneId.Id).Result;
                    Dictionary<string, State> lightStates = scene.LightStates;

                    // Update scenes to use the new color temperature
                    foreach (string lightId in scene.LightStates.Keys)
                    {
                        // Light in scene is also in the light to update list
                        if (lightsToSet.Any(a => a.Id == lightId))
                        {
                            // Light contains a color temperature value which needs to be updated
                            if (lightStates[lightId].On &&
                                (lightStates[lightId].ColorTemperature.HasValue &&
                                Math.Abs(lightStates[lightId].ColorTemperature.Value - colorTemperature) > ScentUpdateThreshold) ||
                                lightStates[lightId].ColorCoordinates != null)
                            {
                                // For white ambiance lights, don't adjust the color temperature when over the allowed threshold
                                if (LightDetails.IsInAllowedColorRange(lightsToSet.First(a => a.Id == lightId).Type, colorTemperature))
                                {
                                    LightCommand lightCommand = new LightCommand()
                                    {
                                        ColorTemperature = colorTemperature,
                                        Brightness = lightStates[lightId].Brightness,
                                        On = true,
                                    };

                                    try
                                    {
                                        HueResults result = client.ModifySceneAsync(sceneId.Id, lightId, lightCommand).Result;
                                    }
                                    catch
                                    {
                                        log.Error($"Exception: '{nameof(ModifyFluxSwitchScenes)}' unable to modify scene ID '{sceneId.Id}' named '{scene.Name}' with color temperature set to '{colorTemperature}'.");
                                    }

                                     //log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' modified scene ID '{sceneId.Id}' named '{scene.Name}' for light '{lightId}' to color temperature '{colorTemperature}'.");

                                    // Limit hub requests to a maximum of 10x per second
                                    Thread.Sleep(5);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Establishes a connection to all Hue Hubs on the network.
        /// </summary>
        private List<KeyValuePair<HueBridge, List<LightDetails>>> ConnectClient(Dictionary<string, string> bridgeIds)
        {
            IBridgeLocator locator = new HttpBridgeLocator();

            IEnumerable<LocatedBridge> bridgeIps = locator.LocateBridgesAsync(TimeSpan.FromSeconds(15)).Result;

            List<KeyValuePair<HueBridge, List<LightDetails>>> clients = new List<KeyValuePair<HueBridge, List<LightDetails>>>();

            foreach (LocatedBridge bridge in bridgeIps)
            {
                LocalHueClient client = new LocalHueClient(bridge.IpAddress);

                string secret = bridgeIds.First(a => a.Key.Equals(bridge.BridgeId, StringComparison.InvariantCultureIgnoreCase)).Value;

                client.Initialize(secret);

                HueBridge hueBridge = new HueBridge();

                hueBridge.Client = client;
                hueBridge.BridgeId = bridge.BridgeId;
                hueBridge.IpAddress = bridge.IpAddress;

                clients.Add(new KeyValuePair<HueBridge, List<LightDetails>>(hueBridge, new List<LightDetails>()));
            }

            return clients;
        }

        /// <summary>
        /// List of lights allowed to be processed as part of Flux updates. Only lights added here may be updated.
        /// </summary>
        /// <param name="lightsToAdd"></param>
        private void AddLights(List<string> lightsToAdd)
        {
            if (lightsToAdd == null || lightsToAdd.Count() == 0)
            {
                throw new ArgumentNullException($"Argument '{nameof(lightsToAdd)}' cannot not be null or empty.");
            }

            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in clients)
            {
                foreach (Light light in client.Key.Client.GetLightsAsync().Result)
                {
                    if (lightsToAdd.Contains(light.UniqueId))
                    {
                        LightDetails.LightType type = LightDetails.TranslateStringToLightType(light.Type);

                        if (type != LightDetails.LightType.WhiteOnly)
                        {
                            LightDetails lightDetail = new LightDetails()
                            {
                                Id = light.Id,
                                Name = light.Name,
                                Type = type,
                            };

                            client.Value.Add(lightDetail);
                        }
                    }
                }
            }
        }
    }
}
