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

        private int lastColorTemperature;

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
                    LastColorTemperature = lastColorTemperature,
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
            lastColorTemperature = 0;

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

                if (DateTime.Now > whenToWake)
                {
                    whenToWake = whenToWake.AddDays(1);
                }

                log.Info($"'{nameof(FluxTimerThread)}' triggering at '{whenToWake.ToShortTimeString()}' in '{(int)(whenToWake - DateTime.Now).TotalMinutes}' minutes for '{fluxRule.Name}' with lights '{String.Join(", ", fluxRule.LightIds)}'.");

                Task.Delay(whenToWake - DateTime.Now, cancellationToken).ContinueWith(tsk => {}).Wait();

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

                // Round the number of minutes to nearest quarter
                const int round = 15;
                double CountRound = (currentSleepDuration.TotalSeconds / round);
                int totalMinutes = (int)Math.Truncate(CountRound + 0.5) * round / 60;

                log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{colorTemperature}' complete; sleeping for '{totalMinutes}' minutes and will resume at '{now + currentSleepDuration}'.");

                // Set updated status prior to invoking callbacks
                this.lastColorTemperature = colorTemperature;
                this.currentSleepDuration = currentSleepDuration;
                this.currentWakeCycle = now + currentSleepDuration;

                // Wait for the next interval which will require an update
                Task.Delay(currentSleepDuration, cancellationToken).ContinueWith(tsk => { }).Wait();
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

            IEnumerable<Light> lights = null;

            try
            {
                lights = client.GetLightsAsync().Result;
            }
            catch (Exception e)
            {
                log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to get lights from client. '{e.Message}' '{e.InnerException}'.");
                return;
            }

            foreach (Light light in lights)
            {
                if (lightsToScan.Any(a => a.Id == light.Id))
                {
                    // Flux color temperature adjustment
                    if (light.State.On &&
                        light.State.ColorMode != "hs" && // only operate on bulbs when in ambiance-lighting mode
                        light.State.ColorTemperature != colorTemperature)
                    {
                        // For white ambiance lights, don't adjust the color temperature when over the allowed threshold
                        if (LightDetails.IsInAllowedColorRange(lightsToScan.First(a => a.Id == light.Id).Type, colorTemperature))
                        {
                            lightsToUpdate.Add(light.Id);
                            log.Debug($"'{nameof(ModifyFluxLights)}' found '{light.Id}', '{light.Name}' with temperature '{light.State.ColorTemperature}' and brightness '{light.State.Brightness}'.");
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

                    log.Info($"'{nameof(ModifyFluxLights)}' set '{lightsToUpdate.Count()}' lights to color temperature '{colorTemperature}' to '{String.Join(", ", lightsToUpdate)}'.");
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
            Dictionary<string, int> scenesModified = new Dictionary<string, int>();

            // Scenes to update
            foreach (Scene sceneId in client.GetScenesAsync().Result)
            {
                // Only modify 'Flux' scenes
                if (sceneId.Name.ToLowerInvariant().Contains("flux") &&
                    sceneId.Name.ToLowerInvariant().Contains("switch"))
                {
                    Scene scene = null;

                    try
                    {
                        scene = client.GetSceneAsync(sceneId.Id).Result;
                    }
                    catch (Exception e)
                    {
                        log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to get scene info from client. '{e.Message}' '{e.InnerException}'.");
                        return;
                    }

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

                                    // Increment the number of lights modified per scene
                                    if (scenesModified.ContainsKey(scene.Name))
                                    {
                                        scenesModified[scene.Name]++;
                                    }
                                    else
                                    {
                                        scenesModified.Add(scene.Name, 1);
                                    }

                                    // Limit hub requests to a maximum of 10x per second
                                    Thread.Sleep(5);
                                }
                            }
                        }
                    }
                }
            }

            if (scenesModified.Count() > 0)
            {
                foreach (KeyValuePair<string, int> pair in scenesModified)
                {
                    log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' modified '{pair.Value}' lights in the scene named '{pair.Key}' to color temperature '{colorTemperature}'.");
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
                    LightDetails.LightType type = LightDetails.TranslateStringToLightType(light.Type);

                    if (lightsToAdd.Contains(light.Name.ToLower()) &&
                        type != LightDetails.LightType.WhiteOnly)
                    {
                        LightDetails lightDetail = new LightDetails()
                        {
                            Id = light.Id,
                            Name = light.Name,
                            Type = type,
                        };

                        client.Value.Add(lightDetail);

                        log.Info($"{nameof(AddLights)} Adding '{light.Name}' which is a '{type}' light for Flux.");
                    }
                    else
                    {
                        log.Info($"{nameof(AddLights)} Skipping '{light.Name}' which is a '{type}' light for Flux.");
                    }
                }
            }
        }
    }
}
