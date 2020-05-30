using log4net;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FluxService
{
    public class MqttSubscriber
    {
        /// <summary>
        /// Callback delegate definitions.
        /// </summary>
        public delegate Task OnEnablementUpdatedCallback(bool enable);
        public delegate void OnLightLevelUpdated(double lightLevel);
        public delegate void OnFluxStatusUpdatedCallback(byte brightness, int colorTemperature);

        /// <summary>
        /// MQTT Client.
        /// </summary>
        private readonly MqttFactory MqttFactory;
        private readonly IMqttClient MqttClient;

        /// <summary>
        /// MQTT Connection Settings.
        /// </summary>
        private readonly MqttConfig MqttConfig = MqttConfig.ParseConfig();

        /// <summary>
        /// Logging
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructor.
        /// </summary>
        public MqttSubscriber(
            OnEnablementUpdatedCallback onEnablementUpdatedCallback, 
            OnLightLevelUpdated onLightLevelUpdatedCallback,
            OnFluxStatusUpdatedCallback onFluxStatusUpdatedCallback)
        {
            if (null == onEnablementUpdatedCallback)
            {
                throw new ArgumentNullException(nameof(onEnablementUpdatedCallback));
            }

            if (null == onLightLevelUpdatedCallback)
            {
                throw new ArgumentNullException(nameof(onLightLevelUpdatedCallback));
            }

            MqttFactory = new MqttFactory();
            MqttClient = MqttFactory.CreateMqttClient();

            // Handle callbacks
            MqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                try
                {
                    string utfString = Encoding.UTF8.GetString(e.ApplicationMessage.Payload, 0, e.ApplicationMessage.Payload.Length);

                    if (e.ApplicationMessage.Topic.Equals($"{MqttConfig.Topic}/set", StringComparison.OrdinalIgnoreCase))
                    {
                        bool enable = Convert.ToBoolean(utfString);
                        onEnablementUpdatedCallback(enable);
                    }
                    else if (e.ApplicationMessage.Topic.Equals($"{MqttConfig.Topic}/lightlevel", StringComparison.OrdinalIgnoreCase))
                    {
                        double lightLevel = Convert.ToDouble(utfString);
                        onLightLevelUpdatedCallback(lightLevel);
                    }
                    else if (e.ApplicationMessage.Topic.Equals($"{MqttConfig.Topic}/status", StringComparison.OrdinalIgnoreCase))
                    {
                        FluxStatus fluxStatus = JsonConvert.DeserializeObject<FluxStatus>(utfString);
                        onFluxStatusUpdatedCallback(fluxStatus.Brightness, fluxStatus.ColorTemperature);
                    }
                }
                catch (Exception)
                {
                }
            });

            // Handle subscription connection
            MqttClient.UseConnectedHandler(async e =>
            {
                Log.Debug($"{nameof(MqttSubscriber)} is connected. Attempting to subscribe to topic '{MqttConfig.Topic}'.");

                // Subscribe to the desired topic when connected
                MqttClientSubscribeResult result = await MqttClient.SubscribeAsync(new TopicFilterBuilder()
                    .WithTopic($"{MqttConfig.Topic}/#")
                    .Build());
            });

            // Handle disconnects
            MqttClient.UseDisconnectedHandler(async e =>
            {
                Log.Debug($"{nameof(MqttSubscriber)} is disconnected. Attempting to reconnect.");

                // Allow time for network connectivy hiccups to be resolved before trying again.
                await Task.Delay(TimeSpan.FromSeconds(1));

                // Reconnect when disconnected
                Connect();
            });
        }

        /// <summary>
        /// Attempt to connect and subscribe to the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public void Connect()
        {
            if (string.IsNullOrEmpty(MqttConfig.BrokerHostname))
            {
                throw new ArgumentNullException(nameof(MqttConfig.BrokerHostname));
            }

            if (string.IsNullOrEmpty(MqttConfig.Username))
            {
                throw new ArgumentNullException(nameof(MqttConfig.Username));
            }

            if (string.IsNullOrEmpty(MqttConfig.Password))
            {
                throw new ArgumentNullException(nameof(MqttConfig.Password));
            }

            // Create TCP-based connection options
            IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(MqttConfig.BrokerHostname)
                .WithCredentials(MqttConfig.Username, MqttConfig.Password)
                .WithCleanSession()
                .Build();

            // Fire and forget to ensure windows service is not blocked on initialization
            // pending successful connection to the MQTT subscription.
            Task task = Task.Factory.StartNew(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                while (!MqttClient.IsConnected)
                {
                    try
                    {
                        await MqttClient.ConnectAsync(mqttClientOptions);
                    }
                    catch (Exception)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
            });
        }

        /// <summary>
        /// Disconnect from the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public async Task Disconnect()
        {
            await MqttClient.DisconnectAsync();
        }

        /// <summary>
        /// Publishes a FluxStatus payload.
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="payload">Payload</param>
        /// <param name="retain">Retain</param>
        /// <returns></returns>
        public async Task PublishFluxStatus(FluxStatus fluxStatus)
        {
            await Publish("status", JsonConvert.SerializeObject(fluxStatus), true);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        /// <param name="topic">Topic</param>
        /// <param name="payload">Payload</param>
        /// <param name="retain">Retain</param>
        /// <returns></returns>
        private async Task Publish(string topic, string payload, bool retain)
        {
            await MqttClient.PublishAsync($"{MqttConfig.Topic}/{topic}", payload, retain);
        }
    }
}
