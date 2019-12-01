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
        private TimeSpan LightTansitionTime;

        private TimeSpan CurrentSleepDuration;

        private DateTime CurrentWakeCycle;

        private int LastColorTemperature = 0;

        private byte LastBrightness = LightDetails.MaxBrightness;

        private Flux Flux = null;

        private List<KeyValuePair<HueBridge, List<LightDetails>>> HueClients = null;

        /// <summary>
        /// Flux Update Worker.
        /// </summary>
        private CancellationTokenSource FluxUpdateWorkerCancellationToken = null;

        /// <summary>
        /// Light Level property
        /// </summary>
        public double LightLevel { get; private set; } = 0.0;

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
        private readonly static Object thisLock = new Object();

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
                    FluxStatus = Flux?.Status ?? new FluxStatus(),
                    On = FluxUpdateWorkerCancellationToken != null && !FluxUpdateWorkerCancellationToken.IsCancellationRequested,
                    LastColorTemperature = LastColorTemperature,
                    LastBrightness = LastBrightness,
                    LastLightlevel = Convert.ToInt32(LightLevel),
                    CurrentSleepDuration = CurrentSleepDuration,
                    CurrentWakeCycle = CurrentWakeCycle,
                };
            }
        }

        /// <summary>
        /// Instiantiates the Flux client.
        /// </summary>
        /// <param name="hueConfig"></param>
        public Hue()
        {
        }

        /// <summary>
        /// Interacts with the Hue client.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="lightLevel"></param>
        public async Task MakeRequest(bool start, double lightLevel)
        {
            LightLevel = lightLevel;

            if (start)
            {
                if (FluxUpdateWorkerCancellationToken == null || FluxUpdateWorkerCancellationToken.IsCancellationRequested)
                {
                    await Start();
                }
            }
            else
            {
                if (HueClients != null && HueClients.Any() && FluxUpdateWorkerCancellationToken != null && !FluxUpdateWorkerCancellationToken.IsCancellationRequested)
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Start the Flux worker.
        /// </summary>
        private async Task Start()
        {
            // Parse the config JSON on the fly
            await SetFluxConfigValues();

            // Create the flux worker thread
            FluxUpdateWorkerCancellationToken = new CancellationTokenSource();
            Task fluxUpdateWorkerTask = Task.Run(() => FluxUpdateThread(FluxUpdateWorkerCancellationToken.Token));
        }

        /// <summary>
        /// Discontinue Flux updates.
        /// </summary>
        private void Stop()
        {
            FluxUpdateWorkerCancellationToken.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal async Task SetFluxConfigValues()
        {
            FluxConfig fluxConfig = FluxConfig.ParseConfig();

            LightTansitionTime = fluxConfig.LightTransitionDuration;

            if (HueClients == null)
            {
                HueClients = await ConnectClient(fluxConfig.BridgeIds);

                await AddLights();
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

            log.Info($"'{nameof(SetFluxConfigValues)}' Sunrise will occur at '{Flux.Sunrise}' and color '{Flux.SunriseColorTemperature}'.");
            log.Info($"'{nameof(SetFluxConfigValues)}' Noon will occur at'{Flux.SolarNoon}' and color '{Flux.SolarNoonTemperature}'.");
            log.Info($"'{nameof(SetFluxConfigValues)}' Sunset will occur at '{Flux.Sunset}' and color '{Flux.SunsetColorTemperature}'.");
            log.Info($"'{nameof(SetFluxConfigValues)}' StopDate will occur at '{Flux.StopDate}' and color '{Flux.StopColorTemperature}'.");

            log.Info($"'{nameof(SetFluxConfigValues)}' Brightness levels to vary between '{MinBrightness}' and '{MaxBrightness}'.");
            log.Info($"'{nameof(SetFluxConfigValues)}' Lightlevels to vary between '{MinLightLevel}' and '{MaxLightLevel}'.");
        }

        /// <summary>
        /// Establishes a connection to all Hue Hubs on the network.
        /// </summary>
        internal async Task<List<KeyValuePair<HueBridge, List<LightDetails>>>> ConnectClient(Dictionary<string, string> bridgeIds)
        {
            IBridgeLocator locator = new HttpBridgeLocator();

            IEnumerable<LocatedBridge> bridgeIps = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(15));

            List<KeyValuePair<HueBridge, List<LightDetails>>> clients = new List<KeyValuePair<HueBridge, List<LightDetails>>>();

            foreach (LocatedBridge bridge in bridgeIps)
            {
                log.Info($"{nameof(ConnectClient)} Found bridge with ID '{bridge.BridgeId}' at '{bridge.IpAddress}'.");

                LocalHueClient client = new LocalHueClient(bridge.IpAddress);

                string secret = bridgeIds.First(a => a.Key.Equals(bridge.BridgeId, StringComparison.InvariantCultureIgnoreCase)).Value;

                client.Initialize(secret);

                HueBridge hueBridge = new HueBridge
                {
                    Client = client,
                    BridgeId = bridge.BridgeId,
                    IpAddress = bridge.IpAddress
                };

                clients.Add(new KeyValuePair<HueBridge, List<LightDetails>>(hueBridge, new List<LightDetails>()));
            }

            return clients;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adjustBrightness"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        internal async Task<TimeSpan> FluxUpdate()
        {
            // Get the color temperature for the given time of day
            int colorTemperature = Flux.GetColorTemperature(DateTime.Now);

            // Get the brightness
            byte brightness = CalculateFluxBrightness();

            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
            {
                // Only process a hub if there are lights in that hub to check
                if (client.Value.Count() > 0)
                {
                    // Send light update commands to lights which are currently 'On'
                    await ModifyFluxLights(client.Key.Client, client.Value, colorTemperature, brightness, LightTansitionTime);

                    // Update the underlying 'Flux' scenes
                    await ModifyFluxSwitchScenes(client.Key.Client, client.Value, colorTemperature, brightness, LightTansitionTime);
                }
            }

            // Get our next sleep duration which is no less than 4 minutes out.
            TimeSpan currentSleepDuration = TimeSpan.FromSeconds(Math.Max(
                TimeSpan.FromMinutes(4).TotalSeconds,
                Flux.GetThreadSleepDuration(DateTime.Now).TotalSeconds));

            // Round the number of minutes to nearest quarter
            const int round = 15;
            double CountRound = (currentSleepDuration.TotalSeconds / round);

            log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{colorTemperature}' and brightness '{brightness}' complete; sleeping for '{Math.Floor(currentSleepDuration.TotalMinutes):00}:{currentSleepDuration.Seconds:00}' and will resume at '{DateTime.Now + currentSleepDuration}'.");

            // Set updated status prior to invoking callbacks
            this.LastColorTemperature = colorTemperature;
            this.LastBrightness = brightness;
            this.CurrentSleepDuration = currentSleepDuration;
            this.CurrentWakeCycle = DateTime.Now + currentSleepDuration;

            return currentSleepDuration;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        internal async Task ModifyFluxLights(HueClient client, List<LightDetails> lightsToScan, int colorTemperature, byte brightness, TimeSpan transitiontime)
        {
            // Lights to update
            IEnumerable<Light> lights = null;

            try
            {
                lights = await client.GetLightsAsync();
            }
            catch (Exception e)
            {
                log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to get lights from client. '{e.Message}' '{e.InnerException}'.");
                return;
            }

            // Exclude lights not to be included in Flux calcuationss
            lights = lights.Where(light =>
                light.State.On &&
                lightsToScan.Any(lightToScan => 
                    lightToScan.Id == light.Id &&
                    lightToScan.AdjustColorTemperatureAllowed));

            Dictionary<byte, List<string>> lightsToUpdate = CalculateLightCommands(lights, colorTemperature, brightness, LastBrightness);

            // Send the light update command
            foreach (KeyValuePair<byte, List<string>> lightsPerBrightnessToUpdate in lightsToUpdate)
            {
                try 
                {
                    LightCommand lightCommand = new LightCommand()
                    {
                        ColorTemperature = colorTemperature,
                        TransitionTime = transitiontime,
                        Brightness = lightsPerBrightnessToUpdate.Key,
                    };

                    HueResults result = await client.SendCommandAsync(lightCommand, lightsPerBrightnessToUpdate.Value);

                    IEnumerable<string> lightNames = lightsPerBrightnessToUpdate.Value
                        .Select(lightId => 
                            lightsToScan.Single(light => 
                                light.Id.Equals(lightId, StringComparison.OrdinalIgnoreCase)).Name)
                        ?.Take(4);

                    log.Info($"'{nameof(ModifyFluxLights)}' set '{lightsPerBrightnessToUpdate.Value.Count()}' lights to color temperature '{colorTemperature}' and brightness '{lightsPerBrightnessToUpdate.Key.ToString()}' for lights '{string.Join(", ", lightNames)}', IDs '{string.Join(", ", lightsPerBrightnessToUpdate.Value)}'.");
                }
                catch (Exception)
                {
                    log.Error($"Exception: '{nameof(ModifyFluxLights)}' sent update request for ColorTemperature '{colorTemperature}' to '{string.Join(", ", lightsPerBrightnessToUpdate.Value)}'.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        internal static Dictionary<byte, List<string>> CalculateLightCommands(IEnumerable<Light> lights, int newColorTemperature, byte newBrightness, byte lastBrightness)
        {
            // Lights to update
            Dictionary<byte, List<string>> lightsCommands = new Dictionary<byte, List<string>>();

            double[] newColorTemperatureAsXY = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(newColorTemperature));

            // Group all lights by common name to ensure all like-named light group have a single, common brightness value.
            IEnumerable<IGrouping<string, Light>> lightGroups = lights
                .GroupBy(light =>
                    light.Name.Trim().LastIndexOf(" ") > 0 ?
                        light.Name.Substring(0, light.Name.Trim().LastIndexOf(" ")) :
                        light.Name);
                    //Regex.Replace(light.Name, @"[\d-]", string.Empty)

            foreach (IGrouping<string, Light> lightGroup in lightGroups)
            {
                byte brightnessMostCommon = lightGroup
                    .GroupBy(x => x.State.Brightness)
                    // Never choose a Zero brightness as the most common.
                    .OrderByDescending(x => x.All(y => y.State.Brightness != 0))
                    // Get the most commonly occurring value, if any.
                    .ThenByDescending(x => x.Count())
                    // Choose the value which matches the new brightness, if present.
                    .ThenByDescending(x => x.All(y => y.State.Brightness == newBrightness))
                    // Prefer the highest value as tie-breaker.
                    .ThenByDescending(x => x.First().State.Brightness)
                    .Select(x => x.Key)
                    .First();

                byte brightnessToSet = 
                (
                    // Last brightness was previously set by Flux.
                    brightnessMostCommon == lastBrightness || 
                    // Brightnes is at or near max brightness.
                    brightnessMostCommon.CompareTo(247) >= 0 ||
                    // Brightness has an invalid brightness value.
                    brightnessMostCommon == 0
                ) ? newBrightness : brightnessMostCommon;

                log.Debug($"'{nameof(CalculateLightCommands)}' {lightGroup.Key} has common brightness value '{brightnessMostCommon}'. Setting to '{brightnessToSet}'.");

                foreach (Light light in lightGroup)
                {
                    if (light.State.On)
                    {
                        bool needToSetBrightness = light.State.Brightness != brightnessToSet;
                        
                        bool needToSetColorTemperature = false;

                        if (light.SupportsColorOrTemperatureChange())
                        {
                            switch (light.State.ColorMode)
                            {
                                case "xy":
                                    {
                                        double xyDifference = Math.Abs(light.State.ColorCoordinates[0] - newColorTemperatureAsXY[0]) + Math.Abs(light.State.ColorCoordinates[1] - newColorTemperatureAsXY[1]);
                                        if (xyDifference > 0.001 && xyDifference < 0.15)
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
                                    // Only set ColorTemp values if there's an allowable change.
                                    needToSetColorTemperature = (light.State.ColorTemperature != newColorTemperature) && light.IsInAllowedColorRange(newColorTemperature);
                                    break;
                            }
                        }

                        // Only send a light adjustment command when needed.
                        if (needToSetColorTemperature || needToSetBrightness)
                        {
                           // byte brightnessToSet = LightDetails.NormalizeBrightness(needToSetBrightness ? newBrightness : light.State.Brightness);

                            if (lightsCommands.ContainsKey(brightnessToSet))
                            {
                                lightsCommands[brightnessToSet].Add(light.Id);
                            }
                            else
                            {
                                lightsCommands[brightnessToSet] = new List<string>() { light.Id };
                            }
                        }
                    }
                }
            }

            return lightsCommands;
        }

        /// <summary>
        /// Flux worker thread
        /// </summary>
        private async void FluxUpdateThread(CancellationToken cancellationToken)
        {
            log.Info($"'{nameof(FluxUpdateThread)}' now running.");

            // Infinite loop until told to stop by master thread
            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan currentSleepDuration = await FluxUpdate();

                // Wait for the next interval which will require an update
                await Task.Delay(currentSleepDuration, cancellationToken).ContinueWith(tsk =>
                {
                    log.Info($"'{nameof(FluxUpdateThread)}' now in '{tsk.Status}'.");
                });
            }

            // We're no longer running so allow another thread to be kicked off later
            log.Info($"'{nameof(FluxUpdateThread)}' now terminating.");
        }

        /// <summary>
        /// Returns a brightness byte value when a lightlevel has been provided to the Flux RESTful service.
        /// </summary>
        /// <returns></returns>
        private byte CalculateFluxBrightness()
        {
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
                double lightLevelPercent = Math.Max(0.0, (LightLevel - this.MinLightLevel) / Math.Max(this.MaxLightLevel - this.MinLightLevel, LightLevel - this.MinLightLevel));

                log.Debug($"LightLevel: {LightLevel}. LightLevel Percent: {lightLevelPercent.ToString()}. Brightness: {(byte)Math.Floor(this.MinBrightness + (this.MaxBrightness - this.MinBrightness) * (1.0 - lightLevelPercent))}.");

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
        private async Task ModifyFluxSwitchScenes(HueClient client, List<LightDetails> lightsToSet, int colorTemperature, byte? brightness, TimeSpan transitiontime)
        {
            Dictionary<string, int> scenesModified = new Dictionary<string, int>();

            IEnumerable<Scene> scenes = null;

            try
            {
                scenes = await client.GetScenesAsync();
            }
            catch(Exception e)
            {
                log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' exception attempting to get all scenes from client. '{e.Message}' '{e.InnerException}'.");
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

                    // Update scenes to use the new color temperature and brightness
                    foreach (string lightId in scene.LightStates.Keys)
                    {
                        if (lightStates[lightId].On)
                        {
                            LightDetails lightDetails = lightsToSet.SingleOrDefault(x => x.Id == lightId);

                            // Only modify lights in the allowed-to-update list
                            if (null != lightDetails)
                            {
                                LightCommand lightCommand = new LightCommand()
                                {
                                    On = true,
                                };

                                if (lightDetails.AdjustColorTemperatureAllowed)
                                {
                                    lightCommand.ColorTemperature = LightDetails.NormalizeColorForAllowedColorRange(lightsToSet.First(a => a.Id == lightId).Type, colorTemperature);
                                }

                                if (lightDetails.AdjustBrightnessAllowed)
                                {
                                    lightCommand.Brightness = brightness ?? lightStates[lightId].Brightness;
                                }
                                else
                                {
                                    lightCommand.Brightness = lightStates[lightId].Brightness;
                                }

                                if (lightCommand.Brightness.HasValue)
                                {
                                    lightCommand.Brightness = lightCommand.Brightness.Value;
                                }

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
            }
        }

        /// <summary>
        /// List of lights allowed to be processed as part of Flux updates. Only lights added here may be updated.
        /// </summary>
        /// <param name="lightsToAdd"></param>
        private async Task AddLights()
        {
            List<LightEntityRegistry> lightEntities = LightEntityRegistry.DeserializeLightObjectGraph();

            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
            {
                IEnumerable<Light> lights = await client.Key.Client.GetLightsAsync();
                foreach (Light light in lights.OrderBy(n => n.Name))
                {
                    LightEntityRegistry lightEntity = lightEntities.FirstOrDefault(x => x.Name.Equals(light.Name, StringComparison.InvariantCultureIgnoreCase));

                    if (null != lightEntity)
                    {
                        LightDetails lightDetail = new LightDetails()
                        {
                            Id = light.Id,
                            Name = light.Name,
                            Type = light.LightType(),
                            AdjustColorTemperatureAllowed = light.SupportsColorOrTemperatureChange(),
                        };

                        client.Value.Add(lightDetail);

                        log.Info($"{nameof(AddLights)} Adding '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{lightDetail.Type}' light for Flux. Temperature Control {lightDetail.AdjustColorTemperatureAllowed}.");
                    }
                    else if (!lightEntities.Any())
                    {
                        LightDetails lightDetail = new LightDetails()
                        {
                            Id = light.Id,
                            Name = light.Name,
                            Type = light.LightType(),
                            AdjustColorTemperatureAllowed = light.SupportsColorOrTemperatureChange(),
                        };

                        client.Value.Add(lightDetail);

                        log.Info($"{nameof(AddLights)} Adding '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{lightDetail.Type}' light for Flux.");
                    }
                    else
                    {
                        log.Info($"{nameof(AddLights)} Skipping '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{light.LightType()}' light for Flux.");
                    }
                }
            }
        }
    }
}
