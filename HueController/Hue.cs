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
using Utilities;

namespace HueController
{
    public class Hue
    {
        public double? LightLevel { get; set; }

        private const int ScentUpdateThreshold = 10;

        private TimeSpan LightTansitionTime;

        private TimeSpan CurrentSleepDuration;

        private DateTime CurrentWakeCycle;

        private int LastColorTemperature;

        private byte? LastBrightness;

        private Flux Flux = null;

        private FluxTimers FluxTimers = null;

        private List<KeyValuePair<HueBridge, List<LightDetails>>> HueClients = null;

        private CancellationTokenSource CancellationToken = null;

        /// <summary>
        /// Hue Configuration Settings
        /// </summary>
        public double MaxLightLevel { get; private set; }

        public double MinLightLevel { get; private set; }

        public byte MaxBrightness { get; private set; }

        public byte MinBrightness { get; private set; }

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
                    FluxStatus = Flux.Status,
                    On = CancellationToken != null && !CancellationToken.IsCancellationRequested,
                    LastColorTemperature = LastColorTemperature,
                    LastBrightness = LastBrightness.HasValue ? LastBrightness.Value : byte.MaxValue,
                    CurrentSleepDuration = CurrentSleepDuration,
                    CurrentWakeCycle = CurrentWakeCycle,
                };
            }
        }

        /// <summary>
        /// Start the Flux worker.
        /// </summary>
        public void Start()
        {
            if (CancellationToken != null && !CancellationToken.IsCancellationRequested)
            {
                throw new ArgumentException($"'{nameof(Start)}' should not be invoked twice without first invoking '{nameof(Stop)}'.");
            }

            // Parse the config JSON on the fly
            SetFluxConfigValues();

            CancellationToken = new CancellationTokenSource();

            Task.Run(() => FluxUpdateThread(CancellationToken.Token), CancellationToken.Token);

            // Parse the timer JSON on the fly
            Dictionary<string, string> lightsToAdd = GetListOfLightsWithIds();
            this.FluxTimers = FluxTimers.Create(lightsToAdd);

            foreach (FluxRule rule in FluxTimers.Rules)
            {
                Task.Run(() => FluxTimerThread(rule, CancellationToken.Token), CancellationToken.Token);
            }
        }

        /// <summary>
        /// Discontinue Flux updates.
        /// </summary>
        public void Stop()
        {
            if (this.HueClients == null || this.HueClients.Count() == 0)
            {
                throw new ArgumentException($"Lights must be added with '{nameof(AddLights)}' prior to invoking '{nameof(Stop)}'.");
            }

            if (CancellationToken == null || CancellationToken.IsCancellationRequested)
            {
                throw new ArgumentException($"'{nameof(Stop)}' should only be invoked after first invoking '{nameof(Start)}'.");
            }

            CancellationToken.Cancel();
        }

        /// <summary>
        /// Instiantiates the Flux client.
        /// </summary>
        /// <param name="hueConfig"></param>
        private Hue()
        {
            LastColorTemperature = 0;
            LastBrightness = byte.MaxValue;
            LightLevel = null;

            Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private void SetFluxConfigValues()
        {
            FluxConfig fluxConfig = FluxConfig.ParseConfig();

            LightTansitionTime = fluxConfig.LightTransitionDuration;

            if (HueClients == null)
            {
                HueClients = ConnectClient(fluxConfig.BridgeIds);

                AddLights(LightEntityRegistry.DeserializeLightObjectGraph());
            }

            if (Flux == null)
            {
                Flux = new Flux();
            }

            Flux.Latitude = fluxConfig.Latitude;
            Flux.Longitude = fluxConfig.Longitude;
            Flux.StopTime = fluxConfig.StopTime;
            Flux.SolarNoonTemperature = fluxConfig.SolarNoonTemperature;
            Flux.StopColorTemperature = fluxConfig.StopColorTemperature;
            Flux.SunriseColorTemperature = fluxConfig.SunriseColorTemperature;
            Flux.SunsetColorTemperature = fluxConfig.SunsetColorTemperature;

            this.MaxBrightness = fluxConfig.MaxBrightness;
            this.MinBrightness = fluxConfig.MinBrightness;
            this.MaxLightLevel = fluxConfig.MaxLightLevel;
            this.MinLightLevel= fluxConfig.MinLightLevel;

            log.Info($"'{nameof(Start)}' Sunrise will occur at '{Flux.Sunrise}' and color '{Flux.SunriseColorTemperature}'.");
            log.Info($"'{nameof(Start)}' Noon will occur at'{Flux.SolarNoon}' and color '{Flux.SolarNoonTemperature}'.");
            log.Info($"'{nameof(Start)}' Sunset will occur at '{Flux.Sunset}' and color '{Flux.SunsetColorTemperature}'.");
            log.Info($"'{nameof(Start)}' StopDate will occur at '{Flux.StopDate}' and color '{Flux.StopColorTemperature}'.");

            log.Info($"'{nameof(Start)}' Brightness levels to vary between '{MinBrightness}' and '{MaxBrightness}'.");
            log.Info($"'{nameof(Start)}' Lightlevels to vary between '{MinLightLevel}' and '{MaxLightLevel}'.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetListOfLightsWithIds()
        {
            Dictionary<string, string> lightNamesToId = new Dictionary<string, string>();

            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
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
                    whenToWake = Flux.Sunrise;
                }
                else if (fluxRule.Sunset)
                {
                    whenToWake = Flux.Sunset;
                }

                if (DateTime.Now > whenToWake)
                {
                    whenToWake = whenToWake.AddDays(1);
                }

                log.Info($"'{nameof(FluxTimerThread)}' triggering at '{whenToWake.ToShortTimeString()}' in '{(int)(whenToWake - DateTime.Now).TotalMinutes}' minutes for '{fluxRule.Name}' with lights '{String.Join(", ", fluxRule.LightIds)}'.");

                Task.Delay(whenToWake - DateTime.Now, cancellationToken).ContinueWith(tsk => {}).Wait();

                if (!cancellationToken.IsCancellationRequested)
                {
                    foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
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
                                    lightCommand.ColorTemperature = Flux.GetColorTemperature(DateTime.Now);
                                }

                                try
                                {
                                    HueResults result = client.Key.Client.SendCommandAsync(lightCommand, lightIds.ToArray()).Result;

                                    log.Info($"'{nameof(FluxTimerThread)}' activated '{fluxRule.Name}' with brightness '{fluxRule.Brightness}' for lights '{String.Join(", ", lightIds)}'.");
                                }
                                catch (Exception e)
                                {
                                    log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to send a light command. '{e.Message}' '{e.InnerException}'.");
                                }
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
                int colorTemperature = Flux.GetColorTemperature(now);

                // Get the brightness
                byte? brightness = CalculateFluxBrightness();

                foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
                {
                    // Only process a hub if there are lights in that hub to check
                    if (client.Value.Count() > 0)
                    {
                        // Send light update commands to lights which are currently 'On'
                        ModifyFluxLights(client.Key.Client, client.Value, colorTemperature, brightness, LightTansitionTime);

                        // Update the underlying 'Flux' scenes
                        ModifyFluxSwitchScenes(client.Key.Client, client.Value, colorTemperature, brightness, LightTansitionTime);
                    }
                }

                // Get our next sleep duration
                TimeSpan currentSleepDuration = Flux.GetThreadSleepDuration(now);

                // Round the number of minutes to nearest quarter
                const int round = 15;
                double CountRound = (currentSleepDuration.TotalSeconds / round);
                int totalMinutes = (int)Math.Truncate(CountRound + 0.5) * round / 60;

                log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{colorTemperature}' and brightness '{brightness}' complete; sleeping for '{totalMinutes}' minutes and will resume at '{now + currentSleepDuration}'.");

                // Set updated status prior to invoking callbacks
                this.LastColorTemperature = colorTemperature;
                this.LastBrightness = brightness;
                this.CurrentSleepDuration = currentSleepDuration;
                this.CurrentWakeCycle = now + currentSleepDuration;

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
        private void ModifyFluxLights(HueClient client, List<LightDetails> lightsToScan, int colorTemperature, byte? brightness, TimeSpan transitiontime)
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

            double[] currentColorTemperatureAsXY = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(colorTemperature));

            foreach (Light light in lights)
            {
                if (lightsToScan.Any(a => a.Id == light.Id && light.State.On))
                {
                    bool needToSetColorTemperature = false;
                    bool needToSetBrightness = brightness.HasValue ? (light.State.Brightness != brightness.Value) : false;

                    switch (light.State.ColorMode)
                    {
                        case "xy":
                            {
                                int ct = ColorConverter.XYToTemperature(light.State.ColorCoordinates);
                                RGB rgb = ColorConverter.MiredToRGB(ct);
                                double[] xy = ColorConverter.RGBtoXY(rgb);

                                // Sum of the X and Y color difference must be within 10% to be considered a match. This ensures
                                // color changes are not overridden by Flux but also allows for XY color values which are still
                                // in the color temperature spectrum to be Flux-controlled.
                                if (Math.Abs(xy[0] - currentColorTemperatureAsXY[0]) + Math.Abs(xy[1] - currentColorTemperatureAsXY[1]) < 0.15)
                                {
                                    needToSetColorTemperature = true;
                                }
                            }
                            break;

                        case "hs":
                            // Hue & Saturation is always to be ignored by Flux.
                            needToSetColorTemperature = false;
                            break;

                        case "ct":
                        default:
                            // Only set ColorTemp values if there is a change.
                            needToSetColorTemperature = (light.State.ColorTemperature != colorTemperature);
                            break;
                    }

                    LightDetails lightToSet = lightsToScan.First(a => a.Id == light.Id);

                    if (needToSetBrightness && lightToSet.Type == LightDetails.LightType.WhiteOnly)
                    {
                        lightsToUpdate.Add(light.Id);
                    }
                    else if (needToSetColorTemperature)
                    {
                        // For white ambiance lights, don't adjust the color temperature when over the allowed threshold
                        if (LightDetails.IsInAllowedColorRange(lightToSet.Type, colorTemperature))
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
                        ColorTemperature = colorTemperature,
                        Brightness = brightness,
                        TransitionTime = transitiontime,
                    };

                    HueResults result = client.SendCommandAsync(lightCommand, lightsToUpdate).Result;

                    log.Info($"'{nameof(ModifyFluxLights)}' set '{lightsToUpdate.Count()}' lights to color temperature '{colorTemperature}' and brightness '{brightness.ToString()}' to '{String.Join(", ", lightsToUpdate)}'.");
                }
                catch (Exception)
                {
                    log.Error($"Exception: '{nameof(ModifyFluxLights)}' sent update request for ColorTemperature '{colorTemperature}' to '{String.Join(", ", lightsToUpdate)}'.");
                }
            }
        }

        /// <summary>
        /// Returns a brightness byte value when a lightlevel has been provided to the Flux RESTful service.
        /// </summary>
        /// <returns></returns>
        private byte? CalculateFluxBrightness()
        {
            if (!LightLevel.HasValue)
            {
                return null;
            }

            //
            // LightLevel value from Hue Motion Sensor to LUX light reading translation
            //
            //                                        Lux                Hue LightLevel
            // Overcast moonless night sky              0.0001                  0
            // Outdoor: Bright moonlight                1                       1
            // Home: Night light                        2                    3000
            // Home: Dimmed light                      10                   10000
            // Home: ‘Cosy’ living room                50                   17000
            // Home: ‘Normal’ non - task light        150                   22000
            // Home: Working / reading                350                   25500
            // Home: Inside daylight                  700                   28500
            // Home: Maximum to avoid glare          2000                   33000
            // Outdoor: Clear daylight            > 10000                 > 40000
            // Outdoor: direct sunlight            120000                   51000

            if (DateTime.Now > Flux.GetSunrise(DateTime.Now) && 
                DateTime.Now < Flux.GetSunset(DateTime.Now))
            {
                // Daytime
                double lightLevelPercent = Math.Max(0.0, (LightLevel.Value - this.MinLightLevel) / Math.Max(this.MaxLightLevel - this.MinLightLevel, LightLevel.Value - this.MinLightLevel));

                return (byte)Math.Floor(this.MinBrightness + (this.MaxBrightness - this.MinBrightness) * (1.0 - lightLevelPercent));
            }
            else
            {
                // Nightime
                return this.MaxBrightness;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToSet"></param>
        /// <param name="colorTemperature"></param>
        private async void ModifyFluxSwitchScenes(HueClient client, List<LightDetails> lightsToSet, int colorTemperature, byte? brightness, TimeSpan transitiontime)
        {
            Dictionary<string, int> scenesModified = new Dictionary<string, int>();

            IEnumerable<Scene> scenes = null;

            try
            {
                scenes = await client.GetScenesAsync();
            }
            catch(Exception e)
            {
                log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' exception attempting to get all scenes  from client. '{e.Message}' '{e.InnerException}'.");
                return;
            }

            scenes = scenes.Where(x =>
                x.Name.ToLowerInvariant().Contains("flux") &&
                (!x.Recycle.HasValue || !x.Recycle.Value));

            // Scenes to update
            foreach (Scene sceneId in scenes)
            { 
                Scene scene = null;

                try
                { 
                    scene = await client.GetSceneAsync(sceneId.Id);
                }
                catch (Exception e)
                {
                    log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' exception attempting to get scene info from client. '{e.Message}' '{e.InnerException}'.");
                }

                if (null != scene)
                { 
                    Dictionary<string, State> lightStates = scene.LightStates;

                    // Update scenes to use the new color temperature
                    foreach (string lightId in scene.LightStates.Keys)
                    {
                        // Light in scene is also in the light to update list
                        if (lightsToSet.Any(a => a.Id == lightId))
                        {
                            if (lightStates[lightId].On)
                            {
                                LightCommand lightCommand = new LightCommand()
                                {
                                    On = true,
                                    ColorTemperature = LightDetails.NormalizeColorForAllowedColorRange(lightsToSet.First(a => a.Id == lightId).Type, colorTemperature),
                                    Brightness = brightness.HasValue ? brightness.Value : lightStates[lightId].Brightness,
                                    //TransitionTime = TimeSpan.FromMilliseconds(1000),
                                };

                                try
                                {
                                    HueResults result = await client.ModifySceneAsync(sceneId.Id, lightId, lightCommand);
                                }
                                catch
                                {
                                    log.Error($"Exception: '{nameof(ModifyFluxSwitchScenes)}' unable to modify scene ID '{sceneId.Id}' named '{scene.Name}' for light id '{lightId}' with color temperature set to '{colorTemperature}' and brightness '{brightness.ToString()}'.");
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

                                // Limit hub requests
                                Thread.Sleep(15);
                            }
                        }
                    }
                }
            }

            if (scenesModified.Count() > 0)
            {
                log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' modified '{scenesModified.Count()}' scenes to color temperature '{colorTemperature}' and brightness '{brightness.ToString()}'.");

                //foreach (KeyValuePair<string, int> pair in scenesModified)
                //{
                //    log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' modified '{pair.Value}' lights in the scene named '{pair.Key}' to color temperature '{colorTemperature}' and brightness '{brightness.ToString()}'.");
                //}
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
                log.Info($"{nameof(ConnectClient)} Found bridge with ID '{bridge.BridgeId}' at '{bridge.IpAddress}'.");

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
            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
            {
                foreach (Light light in client.Key.Client.GetLightsAsync().Result)
                {
                    LightDetails.LightType type = LightDetails.TranslateStringToLightType(light.Type);

                    if (!lightsToAdd.Any() || lightsToAdd.Contains(light.Name.ToLower()))
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
