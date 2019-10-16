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
        private TimeSpan LightTansitionTime;

        private TimeSpan CurrentSleepDuration;

        private DateTime CurrentWakeCycle;

        private int LastColorTemperature;

        private byte? LastBrightness;

        private Flux Flux = null;

        private List<KeyValuePair<HueBridge, List<LightDetails>>> HueClients = null;

        private CancellationTokenSource CancellationToken = null;

        /// <summary>
        /// Light Level property
        /// </summary>
        private double? _lightLevel;
        public double? LightLevel
        {
            get
            {
                return _lightLevel ?? 25000;
            }
            set
            {
                log.Debug($"'{nameof(LightLevel)}' setting value to '{value}'.");
                _lightLevel = value;
            }
        }

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
                    FluxStatus = Flux.Status,
                    On = CancellationToken != null && !CancellationToken.IsCancellationRequested,
                    LastColorTemperature = LastColorTemperature,
                    LastBrightness = LastBrightness ?? byte.MaxValue,
                    LastLightlevel = LightLevel.HasValue ? Convert.ToInt32(LightLevel.Value) : 0,
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
            SetFluxConfigValues().GetAwaiter().GetResult();

            CancellationToken = new CancellationTokenSource();

            Task.Run(() => FluxUpdateThread(CancellationToken.Token), CancellationToken.Token);
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
        internal Hue()
        {
            LastColorTemperature = 0;
            LastBrightness = byte.MaxValue;
            LightLevel = null;

            Start();
        }

        /// <summary>
        /// Instiantiates the Flux client
        /// </summary>
        /// <param name="lastColorTemperature"></param>
        /// <param name="lastBrightness"></param>
        /// <param name="lightLevel"></param>
        internal Hue(int lastColorTemperature, byte lastBrightness, double lightLevel)
        {
            LastColorTemperature = lastColorTemperature;
            LastBrightness = lastBrightness;
            LightLevel = lightLevel;
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
        private async Task<Dictionary<string, string>> GetListOfLightsWithIds()
        {
            Dictionary<string, string> lightNamesToId = new Dictionary<string, string>();

            foreach (KeyValuePair<HueBridge, List<LightDetails>> client in HueClients)
            {
                IEnumerable<Light> lights = await client.Key.Client.GetLightsAsync();

                foreach (Light light in lights)
                {
                    lightNamesToId.Add(light.Name, light.Id);
                }
            }

            return lightNamesToId;
        }
        
        /// <summary>
        /// Flux worker thread
        /// </summary>
        private async Task FluxUpdateThread(CancellationToken cancellationToken)
        {
            log.Info($"'{nameof(FluxUpdateThread)}' now running.");

            bool adjustBrightness = true;

            // Infinite loop until told to stop by master thread
            while (!cancellationToken.IsCancellationRequested)
            {
                adjustBrightness = !adjustBrightness;

                DateTime now = DateTime.Now;

                TimeSpan currentSleepDuration = await FluxUpdate(adjustBrightness, DateTime.Now);

                // Wait for the next interval which will require an update
                Task.Delay(currentSleepDuration, cancellationToken).ContinueWith(tsk => { }).Wait();
            }

            // We're no longer running so allow another thread to be kicked off later
            log.Info($"'{nameof(FluxUpdateThread)}' now terminating.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adjustBrightness"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        internal async Task<TimeSpan> FluxUpdate(bool adjustBrightness, DateTime now)
        {
            // Get the color temperature for the given time of day
            int colorTemperature = Flux.GetColorTemperature(now);

            // Get the brightness
            byte? brightness = adjustBrightness ? CalculateFluxBrightness() : null;

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

            // Get our next sleep duration
            TimeSpan currentSleepDuration = Flux.GetThreadSleepDuration(now);

            // Round the number of minutes to nearest quarter
            const int round = 15;
            double CountRound = (currentSleepDuration.TotalSeconds / round);
            int totalMinutes = (int)Math.Truncate(CountRound + 0.5) * round / 60;

            log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{colorTemperature}' and brightness '{brightness}' complete; sleeping for '{totalMinutes}' minutes and will resume at '{now + currentSleepDuration}'.");

            // Set updated status prior to invoking callbacks
            this.LastColorTemperature = colorTemperature;
            this.LastBrightness = brightness ?? LastBrightness;
            this.CurrentSleepDuration = currentSleepDuration;
            this.CurrentWakeCycle = now + currentSleepDuration;

            return currentSleepDuration;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        private async Task ModifyFluxLights(HueClient client, List<LightDetails> lightsToScan, int colorTemperature, byte? brightness, TimeSpan transitiontime)
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
            lights = lights.Where(light => lightsToScan.Any(a => 
                a.Id == light.Id && 
                light.State.On && 
                a.AdjustColorTemperatureAllowed && 
                (!brightness.HasValue || (brightness.HasValue && a.AdjustBrightnessAllowed)))
            );

            IEnumerable<string> lightsToUpdate = FluxCalculate.CalculateLightsToUpdate(lights, colorTemperature, brightness);

            // Send the light update command
            if (lightsToUpdate.Any())
            {
                try 
                {
                    LightCommand lightCommand = new LightCommand()
                    {
                        ColorTemperature = colorTemperature,
                        TransitionTime = transitiontime,
                    };

                    if (brightness.HasValue)
                    {
                        lightCommand.Brightness = brightness.Value;
                    }

                    HueResults result = await client.SendCommandAsync(lightCommand, lightsToUpdate);

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

                log.Debug($"LightLevel: {LightLevel.Value}. LightLevel Percent: {lightLevelPercent.ToString()}. Brightness: {(byte)Math.Floor(this.MinBrightness + (this.MaxBrightness - this.MinBrightness) * (1.0 - lightLevelPercent))}.");

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
        /// Establishes a connection to all Hue Hubs on the network.
        /// </summary>
        private async Task<List<KeyValuePair<HueBridge, List<LightDetails>>>> ConnectClient(Dictionary<string, string> bridgeIds)
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
                    LightDetails.LightType type = LightDetails.TranslateStringToLightType(light.Type);

                    LightEntityRegistry lightEntity = lightEntities.FirstOrDefault(x => x.Name.Equals(light.Name, StringComparison.InvariantCultureIgnoreCase));

                    if (null != lightEntity)
                    {
                        LightDetails lightDetail = new LightDetails()
                        {
                            Id = light.Id,
                            Name = light.Name,
                            Type = type,
                            AdjustBrightnessAllowed = lightEntity.ControlBrightness,
                            AdjustColorTemperatureAllowed = (type == LightDetails.LightType.WhiteOnly ? false : lightEntity.ControlTemperature),
                        };

                        client.Value.Add(lightDetail);

                        log.Info($"{nameof(AddLights)} Adding '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{type}' light for Flux. Brightness Control {lightDetail.AdjustBrightnessAllowed}. Temperature Control {lightDetail.AdjustColorTemperatureAllowed}.");
                    }
                    else if (!lightEntities.Any())
                    {
                        LightDetails lightDetail = new LightDetails()
                        {
                            Id = light.Id,
                            Name = light.Name,
                            Type = type,
                            AdjustBrightnessAllowed = true,
                            AdjustColorTemperatureAllowed = (type == LightDetails.LightType.WhiteOnly ? false : true),
                        };

                        client.Value.Add(lightDetail);

                        log.Info($"{nameof(AddLights)} Adding '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{type}' light for Flux.");
                    }
                    else
                    {
                        log.Info($"{nameof(AddLights)} Skipping '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{type}' light for Flux.");
                    }
                }
            }
        }
    }
}
