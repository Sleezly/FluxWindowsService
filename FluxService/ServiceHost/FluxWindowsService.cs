using HueController;
using System.Threading.Tasks;

namespace FluxService
{
    public class FluxWindowsService
    {
        /// <summary>
        /// Hue instance.
        /// </summary>
        public readonly Hue Hue;

        /// <summary>
        /// MQTT subscriber.
        /// </summary>
        private readonly MqttSubscriber MqttSubscriber;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FluxWindowsService()
        {
            Hue = new Hue(PublishFluxStatus);
            MqttSubscriber = new MqttSubscriber(OnEnablementUpdatedCallback, OnLightLevelUpdatedCallback, OnFluxStatusUpdatedCallback);
        }

        /// <summary>
        /// Starting point.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            // Connect to the MQTT broker.
            await MqttSubscriber.Connect();
        }

        /// <summary>
        /// Stops the web server.
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// Publishes a new brightness via MQTT.
        /// </summary>
        private async Task PublishFluxStatus(byte brightness, int colorTemperature)
        {
            await MqttSubscriber.PublishFluxStatus(new FluxStatus(brightness, colorTemperature));
        }

        /// <summary>
        /// Handles enablement changes via MQTT subscription.
        /// </summary>
        /// <param name="enabled"></param>
        private async Task OnEnablementUpdatedCallback(bool enabled)
        {
            await Hue.Enable(enabled);
        }

        /// <summary>
        /// Handles light level changes via MQTT subscription.
        /// </summary>
        private void OnLightLevelUpdatedCallback(double lightLevel)
        {
            Hue.LightLevel = lightLevel;
        }

        /// <summary>
        /// Handles brightness changes via MQTT subscription.
        /// </summary>
        private void OnFluxStatusUpdatedCallback(byte brightness, int colorTemperature)
        {
            Hue.LastBrightness = brightness;
            Hue.LastColorTemperature = colorTemperature;
        }
    }
}
