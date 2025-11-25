using log4net;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
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
        public delegate void OnFluxStatusUpdatedCallback(int colorTemperature);

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
            MqttClient.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    string utfString = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.Array, 0, e.ApplicationMessage.PayloadSegment.Array.Length);

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
                        onFluxStatusUpdatedCallback(fluxStatus.ColorTemperature);
                    }
                }
                catch (Exception)
                {
                }

                return Task.CompletedTask;
            };

            // Handle subscription connection
            MqttClient.ConnectedAsync += async e =>
            {
                Log.Debug($"{nameof(MqttSubscriber)} is connected. Attempting to subscribe to topic '{MqttConfig.Topic}'.");

                // Subscribe to the desired topic when connected
                MqttClientSubscribeResult result = await MqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic($"{MqttConfig.Topic}/#")
                    .Build());
            };

            // Handle disconnects
            MqttClient.DisconnectedAsync += async e =>
            {
                // Allow time for network connectivity hiccups to be resolved before trying again.
                await Task.Delay(TimeSpan.FromSeconds(5));

                // Reconnect when disconnected
                Connect();
            };
        }

        /// <summary>
        /// Attempt to connect and subscribe to the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public async void Connect()
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
            MqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(MqttConfig.BrokerHostname)
                .WithCredentials(MqttConfig.Username, MqttConfig.Password)
                .WithCleanSession()
                .Build();

            // Fire and forget to ensure windows service is not blocked on initialization
            // pending successful connection to the MQTT subscription.
            await Task.Factory.StartNew(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                while (!MqttClient.IsConnected)
                {
                    try
                    {
                        await MqttClient.ConnectAsync(mqttClientOptions);
                    }
                    catch (MqttCommunicationException exception)
                    {
                        Log.Debug($"{exception.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    catch (Exception exception)
                    {
                        Log.Debug($"{exception.Message}");
                        break;
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
        private async Task<MqttClientPublishResult> Publish(string topic, string payload, bool retain)
        {
            return await MqttClient.PublishStringAsync($"{MqttConfig.Topic}/{topic}", payload, retain: retain);
        }
    }
}
