using HueApi;
using HueApi.Models;
using HueApi.Models.Requests;
using log4net;
using log4net.Config;
using log4net.Repository;

namespace HueController
{
    public class FluxHue
    {
        /// <summary>
        /// Properties.
        /// </summary>
        public double? LightLevel { get; set; } = null;

        public void UpdateFluxStatus(byte brightness, int colorTemperature)
        {
            Log.Debug($"{nameof(UpdateFluxStatus)} brightness '{brightness}' and color temperature '{colorTemperature}'.");

            LastBrightnesses.Enqueue(brightness);
            LastColorTemperatures.Enqueue(colorTemperature);

            if (LastBrightnesses.Count > MaxPreviousStatusToRetain)
            {
                LastBrightnesses.Dequeue();
            }

            if (LastColorTemperatures.Count > MaxPreviousStatusToRetain)
            {
                LastColorTemperatures.Dequeue();
            }
        }

        /// <summary>
        /// Flux Status.
        /// </summary>
        private const int MaxPreviousStatusToRetain = 3;
        private readonly Queue<Primitives.Brightness> LastBrightnesses = new Queue<Primitives.Brightness>();
        private readonly Queue<Primitives.ColorTemperature> LastColorTemperatures = new Queue<Primitives.ColorTemperature>();

        /// <summary>
        /// MQTT Interaction.
        /// </summary>
        /// <param name="brightness">Brightness</param>
        /// <param name="ColorTemperature">Color Temperature</param>
        /// <returns></returns>
        public delegate Task PublishFluxStatus(byte brightness, int ColorTemperature);
        private readonly PublishFluxStatus PublishStatus;

        /// <summary>
        /// Hue Configuration Settings
        /// </summary>
        private double MaxLightLevel;
        private double MinLightLevel;
        private byte MaxBrightness;
        private byte MinBrightness;
        private TimeSpan LightTansitionTime;

        /// <summary>
        /// Hue Bridge Clients.
        /// </summary>
        private readonly IEnumerable<LocalHueApi> HueClients = GetHueClients();

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
        private readonly FluxCalculate FluxCalculate = new FluxCalculate();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="publishFluxStatus"></param>
        public FluxHue(PublishFluxStatus publishFluxStatus)
        {
            PublishStatus = publishFluxStatus;

            ILoggerRepository logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

        /// <summary>
        /// Interacts with the Hue client.
        /// </summary>
        /// <param name="start"></param>
        public async Task Enable(bool start)
        {
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
            this.MinLightLevel = fluxConfig.MinLightLevel;

            Log.Info($"'{nameof(SetFluxConfigValues)}' Sunrise will occur at '{FluxCalculate.Sunrise}' and color '{FluxCalculate.SunriseColorTemperature}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' Noon will occur at'{FluxCalculate.SolarNoon}' and color '{FluxCalculate.SolarNoonTemperature}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' Sunset will occur at '{FluxCalculate.Sunset}' and color '{FluxCalculate.SunsetColorTemperature}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' StopDate will occur at '{FluxCalculate.StopDate}' and color '{FluxCalculate.StopColorTemperature}'.");

            Log.Info($"'{nameof(SetFluxConfigValues)}' Brightness levels to vary between '{MinBrightness}' and '{MaxBrightness}'.");
            Log.Info($"'{nameof(SetFluxConfigValues)}' Lightlevels to vary between '{MinLightLevel}' and '{MaxLightLevel}'.");
        }

        /// <summary>
        /// Establishes a connection to all Hue Hubs on the network.
        /// </summary>
        private static IEnumerable<LocalHueApi> GetHueClients()
        {
            Dictionary<string, string> bridgeDetails = FluxConfig.ParseConfig().BridgeDetails;

            return bridgeDetails.Select(bridgeInfo =>
            {
                return new LocalHueApi(bridgeInfo.Key, bridgeInfo.Value);
            });
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
                // Must have valid values before initiating an update
                if (LightLevel.HasValue)
                {
                    await FluxUpdate();
                }

                // Get our next sleep duration which is no less than 4 minutes out.
                TimeSpan currentSleepDuration = TimeSpan.FromSeconds(Math.Max(
                    TimeSpan.FromMinutes(4).TotalSeconds,
                    FluxCalculate.GetThreadSleepDuration(DateTime.Now).TotalSeconds));

                // Round the number of minutes to nearest quarter
                const int round = 15;
                double CountRound = (currentSleepDuration.TotalSeconds / round);

                //Log.Info($"'{nameof(FluxUpdateThread)}' activity for color temperature '{LastColorTemperature}' and brightness '{LastBrightness}' complete; sleeping for '{Math.Floor(currentSleepDuration.TotalMinutes):00}:{currentSleepDuration.Seconds:00}' and will resume at '{DateTime.Now + currentSleepDuration}'.");
                Log.Info($"'{nameof(FluxUpdateThread)}' now sleeping for '{(currentSleepDuration.Hours > 1 ? $"{currentSleepDuration.Hours}:" : string.Empty)}{currentSleepDuration.Minutes:00}:{currentSleepDuration.Seconds:00}'. Will resume at '{DateTime.Now + currentSleepDuration}'.");

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
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task FluxUpdate()
        {
            // Get the color temperature for the given time of day
            Primitives.ColorTemperature colorTemperature = FluxCalculate.GetColorTemperature(DateTime.Now);

            // Get the brightness
            Primitives.Brightness brightness = CalculateFluxBrightness(LightLevel.Value);

            Log.Info($"'{nameof(FluxUpdate)}' activity for color temperature '{colorTemperature}' and brightness '{brightness}' now starting.");

            foreach (LocalHueApi hueClient in HueClients)
            {
                try
                {
                    HueResponse<Light> response = await hueClient.Light.GetAllAsync();
                    IEnumerable<Light> lights = response.Data;

                    // Send light update commands to lights which are currently 'On'
                    await ModifyFluxLights(hueClient, lights, colorTemperature, brightness);

                    // Update the underlying 'Flux' scenes
                    await ModifyFluxSwitchScenes(hueClient, lights, colorTemperature, brightness);
                }
                catch (Exception e)
                {
                    Log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to get lights from client. '{e.Message}' '{e.InnerException}'.");
                }
            };

            // Publish the updated status values so they are retained
            await PublishStatus(brightness, colorTemperature);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToSet"></param>
        /// <param name="colorTemperature"></param>
        internal async Task ModifyFluxSwitchScenes(
            LocalHueApi client,
            IEnumerable<Light> lights,
            Primitives.ColorTemperature colorTemperature,
            Primitives.Brightness newBrightness)
        {
            try
            {
                HueResponse<Scene> response = await client.Scene.GetAllAsync();
                IEnumerable<Scene> scenes = response.Data;

                scenes = scenes
                   .Where(x => x.Metadata.Name.ToLowerInvariant().Contains("flux"))
                   .OrderBy(scene => scene.Metadata.Name);

                // Scenes to update
                foreach (Scene scene in scenes)
                {
                    if (null != scene && scene.Actions.Any(action => action.Action.ColorTemperature != null && action.Action.ColorTemperature != colorTemperature))
                    {
                        UpdateScene updateScene = new UpdateScene()
                        {
                            Speed = scene.Speed,
                            Actions = scene.Actions
                        };

                        foreach (SceneAction action in updateScene.Actions)
                        {
                            if (action.Action.ColorTemperature != null)
                            {
                                action.Action.ColorTemperature = colorTemperature;
                            }
                        }

                        Log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' modified '{scene.Metadata.Name}' to color temperature '{colorTemperature}' and brightness '{newBrightness}'.");

                        await client.Scene.UpdateAsync(scene.Id, updateScene);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug($"'{nameof(ModifyFluxSwitchScenes)}' exception attempting to get all scenes from client. '{e.Message}' '{e.InnerException}'.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        private async Task ModifyFluxLights(
            LocalHueApi hueClient,
            IEnumerable<Light> lights,
            Primitives.ColorTemperature newColorTemperature,
            Primitives.Brightness newBrightness)
        {
            lights = lights
                .Where(light => light.On.IsOn)
                .Where(light => light.IsFluxControlled())
                .Where(light => light.ColorTemperature != null && light.ColorTemperature != newColorTemperature)
                .OrderBy(light => light.Metadata.Name);

            // Send the light update command
            foreach (Light light in lights)
            {
                try
                {
                    Log.Debug($"'{nameof(ModifyFluxLights)}' setting flux light for '{light.Metadata.Name}' from '{light.ColorTemperature.Mirek}' to '{newColorTemperature}'.");

                    await hueClient.Light.UpdateAsync(light.Id, new UpdateLight()
                    {
                        ColorTemperature = newColorTemperature,
                    });

                }
                catch (Exception e)
                {
                    Log.Debug($"'{nameof(ModifyFluxLights)}' exception attempting to get lights from client. '{e.Message}' '{e.InnerException}'");
                }
            }
        }

        /// <summary>
        /// Returns a brightness byte value when a lightlevel has been provided to the Flux RESTful service.
        /// </summary>
        /// <returns></returns>
        private byte CalculateFluxBrightness(double lightLevel)
        {
            return this.MaxBrightness;

            ////
            //// LightLevel value from Hue Motion Sensor to LUX light reading translation
            ////
            ////                                        Lux                Hue LightLevel
            //// Overcast moonless night sky              0.0001                  0
            //// Outdoor: Bright moonlight                1                       1
            //// Home: Night light                        2                    3000
            //// Home: Dimmed light                      10                   10000
            //// Home: ‘Cosy’ living room                50                   17000
            //// Home: ‘Normal’ non - task light        150                   22000
            //// Home: Working / reading                350                   25500
            //// Home: Inside daylight                  700                   28500
            //// Home: Maximum to avoid glare          2000                   33000
            //// Outdoor: Clear daylight            > 10000                 > 40000
            //// Outdoor: direct sunlight            120000                   51000

            //if (DateTime.Now > FluxCalculate.GetSunrise(DateTime.Now) && 
            //    DateTime.Now < FluxCalculate.GetSunset(DateTime.Now))
            //{
            //    // Daytime
            //    double lightLevelPercent = Math.Max(0.0, (lightLevel - MinLightLevel) / Math.Max(MaxLightLevel - MinLightLevel, lightLevel - MinLightLevel));

            //    Log.Debug($"LightLevel: {lightLevel}. LightLevel Percent: {lightLevelPercent.ToString()}. Calculated Brightness: {(byte)Math.Floor(MinBrightness + (MaxBrightness - MinBrightness) * (1.0 - lightLevelPercent))}.");

            //    return (byte)Math.Floor(MinBrightness + (MaxBrightness - MinBrightness) * (1.0 - lightLevelPercent));
            //}
            //else
            //{
            //    // Nightime
            //    return this.MaxBrightness;
            //}
        }
    }
}
