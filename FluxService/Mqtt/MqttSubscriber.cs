using log4net;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
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

            MqttFactory mqttFactory = new MqttFactory();
            MqttClient = mqttFactory.CreateMqttClient();

            MqttClient.ApplicationMessageReceivedAsync += e =>
            {
                return HandleApplicationMessageReceived(e, onEnablementUpdatedCallback, onLightLevelUpdatedCallback, onFluxStatusUpdatedCallback);
            };

            MqttClient.ConnectedAsync += HandleConnected;

            MqttClient.DisconnectedAsync += HandleDisconnected;
        }

        /// <summary>
        /// Handle incoming mesages.
        /// </summary>
        /// <param name="e"><see cref="MqttApplicationMessageReceivedEventArgs"/>.</param>
        /// <param name="onEnablementUpdatedCallback"><see cref="OnEnablementUpdatedCallback"/>.</param>
        /// <param name="onLightLevelUpdatedCallback"><see cref="OnLightLevelUpdated"/>.</param>
        /// <param name="onFluxStatusUpdatedCallback"><see cref="OnFluxStatusUpdatedCallback"/>.</param>
        /// <returns></returns>
        private async Task HandleApplicationMessageReceived(
            MqttApplicationMessageReceivedEventArgs e,
            OnEnablementUpdatedCallback onEnablementUpdatedCallback,
            OnLightLevelUpdated onLightLevelUpdatedCallback,
            OnFluxStatusUpdatedCallback onFluxStatusUpdatedCallback)
        {
            try
            {
                ArraySegment<byte> payloadBytes = e.ApplicationMessage.PayloadSegment;
                string utfString = Encoding.UTF8.GetString(payloadBytes);
                string topic = e.ApplicationMessage.Topic ?? string.Empty;

                if (topic.Equals($"{MqttConfig.Topic}/set", StringComparison.OrdinalIgnoreCase))
                {
                    bool enable = Convert.ToBoolean(utfString);
                    await onEnablementUpdatedCallback(enable);
                }
                else if (topic.Equals($"{MqttConfig.Topic}/lightlevel", StringComparison.OrdinalIgnoreCase))
                {
                    double lightLevel = Convert.ToDouble(utfString);
                    onLightLevelUpdatedCallback(lightLevel);
                }
                else if (topic.Equals($"{MqttConfig.Topic}/status", StringComparison.OrdinalIgnoreCase))
                {
                    FluxStatus fluxStatus = JsonConvert.DeserializeObject<FluxStatus>(utfString);
                    onFluxStatusUpdatedCallback(fluxStatus.ColorTemperature);
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Error handling application message: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle subscription connection.
        /// </summary>
        /// <param name="e"><see cref="MqttClientConnectedEventArgs"/>.</param>
        /// <returns><see cref="Task"/>.</returns>
        private async Task HandleConnected(MqttClientConnectedEventArgs e)
        {
            Log.Debug($"{nameof(MqttSubscriber)} is connected. Attempting to subscribe to topic '{MqttConfig.Topic}'.");

            try
            {
                MqttTopicFilterBuilder filterBuilder = new MqttTopicFilterBuilder();
                await MqttClient.SubscribeAsync(filterBuilder.WithTopic($"{MqttConfig.Topic}/#").Build());
            }
            catch (Exception ex)
            {
                Log.Debug($"Failed to subscribe: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle subscription disconnect.
        /// </summary>
        /// <param name="e"><see cref="MqttClientDisconnectedEventArgs"/>.</param>
        /// <returns><see cref="Task"/>.</returns>
        private async Task HandleDisconnected(MqttClientDisconnectedEventArgs e)
        {
            // Allow time for network connectivity hiccups to be resolved before trying again.
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Reconnect when disconnected
            Connect();
        }

        /// <summary>
        /// Attempt to connect and subscribe to the MQTT broker.
        /// </summary>
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

            MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder();
            MqttClientOptions mqttClientOptions = optionsBuilder
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
        public async Task Disconnect()
        {
            await MqttClient.DisconnectAsync();
        }

        /// <summary>
        /// Publishes a FluxStatus payload.
        /// </summary>
        public async Task PublishFluxStatus(FluxStatus fluxStatus)
        {
            await Publish("status", JsonConvert.SerializeObject(fluxStatus), true);
        }

        /// <summary>
        /// Publishes a message.
        /// </summary>
        private async Task Publish(string topic, string payload, bool retain)
        {
            MqttApplicationMessageBuilder messageBuilder = new MqttApplicationMessageBuilder();
            MqttApplicationMessage message = messageBuilder
                .WithTopic($"{MqttConfig.Topic}/{topic}")
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .Build();

            await MqttClient.PublishAsync(message);
        }
    }
}
