﻿using HueController.Utilities;
using log4net;
using Q42.HueApi;
using Q42.HueApi.Models;
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
        /// <summary>
        /// Status
        /// </summary>
        public Primitives.ColorTemperature ColorTemperature { get; private set; }
        public byte? Brightness { get; private set; } = null;
        public double? LightLevel { get; private set; } = null;
        public TimeSpan? CurrentSleepDuration { get; private set; } = null;
        public DateTime? CurrentWakeCycle { get; private set; } = null;

        /// <summary>
        /// Hue Configuration Settings
        /// </summary>
        public double MaxLightLevel { get; private set; }
        public double MinLightLevel { get; private set; }
        public byte MaxBrightness { get; private set; }
        public byte MinBrightness { get; private set; }
        public TimeSpan LightTansitionTime { get; private set; }

        /// <summary>
        /// Hue Bridge Clients.
        /// </summary>
        private readonly IEnumerable<HueClient> HueClients = GetHueClients();

        /// <summary>
        /// Flux Update Worker.
        /// </summary>
        private Task FluxUpdateWorkerTask = null;
        private CancellationTokenSource FluxUpdateWorkerCancellationToken = null;

        /// <summary>
        /// Logging
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Fluxer.
        /// </summary>
        private readonly Flux Flux = new Flux();

        /// <summary>
        /// Status retrival property.
        /// </summary>
        public HueStatus GetStatus()
        {
            return new HueStatus()
            {
                FluxStatus = Flux.GetStatus(),
                On = FluxUpdateWorkerTask != null && FluxUpdateWorkerCancellationToken != null && !FluxUpdateWorkerCancellationToken.IsCancellationRequested,
                LastColorTemperature = ColorTemperature,
                LastBrightness = Brightness,
                LastLightlevel = Convert.ToInt32(LightLevel),
                CurrentSleepDuration = CurrentSleepDuration,
                CurrentWakeCycle = CurrentWakeCycle,
            };
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
                Start();
            }
            else
            {
                await Stop();
            }
        }

        /// <summary>
        /// Start the Flux worker.
        /// </summary>
        private void Start()
        {
            if (FluxUpdateWorkerCancellationToken == null ||
                FluxUpdateWorkerCancellationToken.IsCancellationRequested ||
                FluxUpdateWorkerTask == null)
            {
                // Parse the config JSON on the fly
                SetFluxConfigValues();

                // Create the flux worker thread
                FluxUpdateWorkerCancellationToken = new CancellationTokenSource();
                FluxUpdateWorkerTask = Task.Run(() => FluxUpdateThread(FluxUpdateWorkerCancellationToken.Token));
            }
        }

        /// <summary>
        /// Discontinue Flux updates.
        /// </summary>
        private async Task Stop()
        {
            if (HueClients != null &&
                HueClients.Any() &&
                FluxUpdateWorkerTask != null && 
                FluxUpdateWorkerCancellationToken != null &&
                !FluxUpdateWorkerCancellationToken.IsCancellationRequested)
            {
                FluxUpdateWorkerCancellationToken.Cancel();

                await FluxUpdateWorkerTask;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal void SetFluxConfigValues()
        {
            FluxConfig fluxConfig = FluxConfig.ParseConfig();

            LightTansitionTime = fluxConfig.LightTransitionDuration;

            this.MaxBrightness = fluxConfig.MaxBrightness;
            this.MinBrightness = fluxConfig.MinBrightness;
            this.MaxLightLevel = fluxConfig.MaxLightLevel;
            this.MinLightLevel= fluxConfig.MinLightLevel;

            Log.Info($"'{nameof(SetFluxConfigValues)}' Sunrise will occur at '{Flux.Sunrise}' and color '{Flux.SunriseColorTemperature}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' Noon will occur at'{Flux.SolarNoon}' and color '{Flux.SolarNoonTemperature}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' Sunset will occur at '{Flux.Sunset}' and color '{Flux.SunsetColorTemperature}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' StopDate will occur at '{Flux.StopDate}' and color '{Flux.StopColorTemperature}'.");

            Log.Info($"'{nameof(SetFluxConfigValues)}' Brightness levels to vary between '{MinBrightness}' and '{MaxBrightness}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' Lightlevels to vary between '{MinLightLevel}' and '{MaxLightLevel}'.");
        }

        /// <summary>
        /// Establishes a connection to all Hue Hubs on the network.
        /// </summary>
        private static IEnumerable<HueClient> GetHueClients()
        {
            Dictionary<string, string> bridgeDetails = FluxConfig.ParseConfig().BridgeDetails;

            return bridgeDetails.Select(bridgeInfo =>
            {
                LocalHueClient localHueClient = new LocalHueClient(bridgeInfo.Key);
                localHueClient.Initialize(bridgeInfo.Value);
                return localHueClient;
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adjustBrightness"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private async Task FluxUpdate()
        {
            // Get the color temperature for the given time of day
            Primitives.ColorTemperature colorTemperature = Flux.GetColorTemperature(DateTime.Now);

            // Get the brightness
            byte brightness = CalculateFluxBrightness();

            foreach (HueClient hueClient in HueClients)
            {
                IEnumerable<Light> lights = null;

                try
                {
                    lights = await hueClient.GetLightsAsync();
                }
                catch (Exception e)
                {
                    Log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to get lights from client. '{e.Message}' '{e.InnerException}'.");
                    return;
                }

                // Send light update commands to lights which are currently 'On'
                await ModifyFluxLights(hueClient, lights, colorTemperature, brightness, Brightness ?? 0, LightTansitionTime);

                // Update the underlying 'Flux' scenes
                await ModifyFluxSwitchScenes(hueClient, lights, colorTemperature, brightness);
            }

            // Set updated status prior to invoking callbacks
            this.ColorTemperature = colorTemperature;
            this.Brightness = brightness;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        private static async Task ModifyFluxLights(HueClient hueClient, IEnumerable<Light> lights, Primitives.ColorTemperature colorTemperature, byte brightness, byte lastBrightness, TimeSpan transitiontime)
        {
            lights = lights.Where(light =>
                light.State.On &&
                light.IsFluxControlled());

            Dictionary<byte, List<string>> lightsToUpdate = CalculateLightCommands(lights, colorTemperature, brightness, lastBrightness);

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

                    HueResults result = await hueClient.SendCommandAsync(lightCommand, lightsPerBrightnessToUpdate.Value.ToArray());

                    IEnumerable<string> lightNames = lightsPerBrightnessToUpdate.Value
                        .Select(lightId =>
                            lights.Single(light =>
                                light.Id.Equals(lightId, StringComparison.OrdinalIgnoreCase)).Name)
                        ?.Take(4);

                    Log.Info($"'{nameof(ModifyFluxLights)}' set '{lightsPerBrightnessToUpdate.Value.Count()}' lights to color temperature '{colorTemperature}' and brightness '{lightsPerBrightnessToUpdate.Key.ToString()}' for lights '{string.Join(", ", lightNames)}', IDs '{string.Join(", ", lightsPerBrightnessToUpdate.Value)}'.");
                }
                catch (Exception exception)
                {
                    Log.Error($"Exception: '{nameof(ModifyFluxLights)}' sent update request for ColorTemperature '{colorTemperature}' to '{string.Join(", ", lightsPerBrightnessToUpdate.Value)}'. {exception.Message}");
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
        internal static Dictionary<byte, List<string>> CalculateLightCommands(IEnumerable<Light> lights, Primitives.ColorTemperature newColorTemperature, byte newBrightness, byte lastBrightness)
        {
            // Lights to update
            Dictionary<byte, List<string>> lightsCommands = new Dictionary<byte, List<string>>();

            //double[] newColorTemperatureAsXY = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(newColorTemperature));
            double[] newColorTemperatureAsXY = newColorTemperature.XY;

            // Group all lights by common name to ensure all like-named light group have a single, common brightness value.
            IEnumerable<IGrouping<string, Light>> lightGroups = lights
                .GroupBy(light =>
                    light.Name.Trim().LastIndexOf(" ") > 0 ?
                        light.Name.Substring(0, light.Name.Trim().LastIndexOf(" ")) :
                        light.Name)
                .OrderBy(lightGroup => lightGroup.Key);

            foreach (IGrouping<string, Light> lightGroup in lightGroups)
            {
                byte brightnessMostCommon = lightGroup
                    .GroupBy(x => x.State.Brightness)
                    // Never choose a Zero brightness as the most common.
                    .OrderByDescending(x => x.All(y => y.State.Brightness != 0))
                    // Prefer the brightness which matches the previous brightness value to help stay in sync with Flux.
                    .ThenByDescending(x => x.All(y => y.State.Brightness == lastBrightness))
                    // Tie breaker for the more common value, if any.
                    .ThenByDescending(x => x.Count())
                    // Prefer the highest value as final tie-breaker.
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

                Log.Debug($"'{nameof(CalculateLightCommands)}' {lightGroup.Key} has common brightness value '{brightnessMostCommon}'. Setting to '{brightnessToSet}'.");

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
                                    needToSetColorTemperature = (new Primitives.ColorTemperature(light.State.ColorTemperature.Value) != newColorTemperature) && newColorTemperature.IsInAllowedColorRange(light.GetLightType());
                                    break;
                            }
                        }

                        // Only send a light adjustment command when needed.
                        if ((needToSetColorTemperature && light.ControlTemperature()) || (needToSetBrightness && light.ControlBrightness()))
                        {
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
        private async Task FluxUpdateThread(CancellationToken cancellationToken)
        {
            Log.Info($"'{nameof(FluxUpdateThread)}' now running.");

            // Infinite loop until told to stop by master thread
            while (!cancellationToken.IsCancellationRequested)
            {
                await FluxUpdate();

                // Get our next sleep duration which is no less than 4 minutes out.
                TimeSpan currentSleepDuration = TimeSpan.FromSeconds(Math.Max(
                    TimeSpan.FromMinutes(4).TotalSeconds,
                    Flux.GetThreadSleepDuration(DateTime.Now).TotalSeconds));

                // Round the number of minutes to nearest quarter
                const int round = 15;
                double CountRound = (currentSleepDuration.TotalSeconds / round);

                this.CurrentSleepDuration = currentSleepDuration;
                this.CurrentWakeCycle = DateTime.Now + currentSleepDuration;

                Log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{ColorTemperature}' and brightness '{Brightness}' complete; sleeping for '{Math.Floor(currentSleepDuration.TotalMinutes):00}:{currentSleepDuration.Seconds:00}' and will resume at '{DateTime.Now + currentSleepDuration}'.");

                // Wait for the next interval which will require an update
                await Task.Delay(currentSleepDuration, cancellationToken).ContinueWith(tsk =>
                {
                    Log.Info($"'{nameof(FluxUpdateThread)}' now in '{tsk.Status}'.");
                });
            }

            // We're no longer running so allow another thread to be kicked off later
            Log.Info($"'{nameof(FluxUpdateThread)}' now terminating.");
        }

        /// <summary>
        /// Returns a brightness byte value when a lightlevel has been provided to the Flux RESTful service.
        /// </summary>
        /// <returns></returns>
        private byte CalculateFluxBrightness()
        {
            if (!LightLevel.HasValue)
            {
                throw new ArgumentNullException(nameof(LightLevel));
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
                double lightLevelPercent = Math.Max(0.0, (LightLevel.Value - MinLightLevel) / Math.Max(MaxLightLevel - MinLightLevel, LightLevel.Value - MinLightLevel));

                Log.Debug($"LightLevel: {LightLevel}. LightLevel Percent: {lightLevelPercent.ToString()}. Brightness: {(byte)Math.Floor(MinBrightness + (MaxBrightness - MinBrightness) * (1.0 - lightLevelPercent))}.");

                return (byte)Math.Floor(MinBrightness + (MaxBrightness - MinBrightness) * (1.0 - lightLevelPercent));
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
        private static async Task ModifyFluxSwitchScenes(HueClient client, IEnumerable<Light> lights, Primitives.ColorTemperature colorTemperature, byte? brightness)
        {
            Dictionary<string, int> scenesModified = new Dictionary<string, int>();

            IEnumerable<Scene> scenes = null;

            try
            {
                scenes = await client.GetScenesAsync();
            }
            catch (Exception e)
            {
                Log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' exception attempting to get all scenes from client. '{e.Message}' '{e.InnerException}'.");
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
                    Log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' exception attempting to get scene info from client. '{e.Message}' '{e.InnerException}'.");
                }

                if (null != scene)
                {
                    Dictionary<string, State> lightStates = scene.LightStates;

                    // Update scenes to use the new color temperature and brightness
                    foreach (string lightId in scene.LightStates.Keys)
                    {
                        Light light = lights.Get(lightId);


                        if (lightStates[lightId].On && light.IsFluxControlled())
                        {
                            LightCommand lightCommand = new LightCommand()
                            {
                                On = true,
                            };

                            if (light.ControlTemperature())
                            {
                                lightCommand.ColorTemperature = colorTemperature.NormalizeColorForAllowedColorRange(light.GetLightType());
                            }

                            if (light.ControlBrightness())
                            {
                                lightCommand.Brightness = brightness ?? lightStates[lightId].Brightness;
                            }
                            else
                            {
                                lightCommand.Brightness = lightStates[lightId].Brightness;
                            }

                            try
                            {
                                if ((lightCommand.ColorTemperature.HasValue && lightStates[lightId].ColorTemperature != lightCommand.ColorTemperature.Value) ||
                                    (lightCommand.Brightness.HasValue && lightStates[lightId].Brightness != lightCommand.Brightness.Value))
                                {
                                    HueResults result = await client.ModifySceneAsync(sceneId.Id, lightId, lightCommand);

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
                                    await Task.Delay(TimeSpan.FromMilliseconds(15));
                                }
                            }
                            catch (Exception exception)
                            {
                                Log.Error($"Exception: '{nameof(ModifyFluxSwitchScenes)}' unable to modify scene ID '{sceneId.Id}' named '{scene.Name}' for light id '{lightId}' with color temperature set to '{colorTemperature}' and brightness '{brightness.ToString()}'. {exception.Message}");

                                // Limit hub requests
                                await Task.Delay(TimeSpan.FromMilliseconds(100));
                            }
                        }
                    }
                }
            }

            Log.Info($"'{nameof(ModifyFluxSwitchScenes)}' modified '{scenesModified.Count()}' scenes to color temperature '{colorTemperature}' and brightness '{brightness.ToString()}'.");
        }

        /// <summary>
        /// List of lights allowed to be processed as part of Flux updates. Only lights added here may be updated.
        /// </summary>
        /// <param name="lightsToAdd"></param>
        //private async Task<IEnumerable<LightDetails>> GetLightDetails(HueClient hueClient)
        //{
        //    IEnumerable<LightEntityRegistry> lightEntities = await LightEntityRegistry.DeserializeLightObjectGraph();

        //    IEnumerable<Light> lights = await hueClient.GetLightsAsync();

        //    return lights
        //        .OrderBy(n => n.Name)
        //        .Select(light =>
        //        {
        //            LightEntityRegistry lightEntity = lightEntities.SingleOrDefault(x => x.Name.Equals(light.Name, StringComparison.InvariantCultureIgnoreCase));

        //            if (null != lightEntity)
        //            {
        //                if (lightEntity.ControlBrightness || lightEntity.ControlTemperature)
        //                {
        //                    log.Info($"{nameof(GetLightDetails)} Adding '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{light.LightType()}' light for Flux.");
                           
        //                    return new LightDetails()
        //                    {
        //                        Id = light.Id,
        //                        Name = light.Name,
        //                        Type = light.LightType(),
        //                        AdjustBrightnessAllowed = lightEntity.ControlBrightness,
        //                        AdjustColorTemperatureAllowed = lightEntity.ControlTemperature && light.SupportsColorOrTemperatureChange(),
        //                    };
        //                }
        //            }
        //            else
        //            {
        //                log.Info($"{nameof(GetLightDetails)} Adding '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{light.LightType()}' light for Flux.");
                    
        //                return new LightDetails()
        //                {
        //                    Id = light.Id,
        //                    Name = light.Name,
        //                    Type = light.LightType(),
        //                    AdjustBrightnessAllowed = true,
        //                    AdjustColorTemperatureAllowed = light.SupportsColorOrTemperatureChange(),
        //                };
        //            }

        //            log.Info($"{nameof(GetLightDetails)} Skipping '{light.Name}' '{light.Id}' '{light.UniqueId}' which is a '{light.LightType()}' light.");
        //            return null;
        //        })
        //        .Where(lightDetail => null != lightDetail);
        //}
    }
}
