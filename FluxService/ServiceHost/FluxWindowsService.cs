using HueController;
using System.Threading.Tasks;

namespace FluxService
{
    public class FluxWindowsService
    {
        /// <summary>
        /// Hue instance.
        /// </summary>
        private FluxHue Hue = null;

        /// <summary>
        /// MQTT subscriber.
        /// </summary>
        private MqttSubscriber MqttSubscriber = null;

        /// <summary>
        /// Starting point.
        /// </summary>
        /// <returns></returns>
        public void Start()
        {
            Hue = new FluxHue(PublishFluxStatus);
            MqttSubscriber = new MqttSubscriber(OnEnablementUpdatedCallback, OnLightLevelUpdatedCallback, OnFluxStatusUpdatedCallback);

            // Connect to the MQTT broker.
            MqttSubscriber?.Connect();
        }

        /// <summary>
        /// Stops the web server.
        /// </summary>
        public async Task Stop()
        {
            await MqttSubscriber?.Disconnect();
        }

        /// <summary>
        /// Publishes a new brightness via MQTT.
        /// </summary>
        private async Task PublishFluxStatus(int colorTemperature)
        {
            await MqttSubscriber?.PublishFluxStatus(new FluxStatus(colorTemperature));
        }

        /// <summary>
        /// Handles enablement changes via MQTT subscription.
        /// </summary>
        /// <param name="enabled"></param>
        private async Task OnEnablementUpdatedCallback(bool enabled)
        {
            await Hue?.Enable(enabled);
        }

        /// <summary>
        /// Handles light level changes via MQTT subscription.
        /// </summary>
        private void OnLightLevelUpdatedCallback(double lightLevel)
        {
            if (null != Hue)
            {
                Hue.LightLevel = lightLevel;
            }
        }

        /// <summary>
        /// Handles brightness changes via MQTT subscription.
        /// </summary>
        private void OnFluxStatusUpdatedCallback(int colorTemperature)
        {
            Hue?.UpdateFluxStatus(colorTemperature);
        }
    }
}
