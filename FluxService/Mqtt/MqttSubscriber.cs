using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
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
        
        /// <summary>
        /// MQTT Client.
        /// </summary>
        private readonly MqttFactory MqttFactory;
        private readonly IMqttClient MqttClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MqttSubscriber(OnEnablementUpdatedCallback onEnablementUpdatedCallback, OnLightLevelUpdated onLightLevelUpdatedCallback)
        {
            if (null == onEnablementUpdatedCallback)
            {
                throw new ArgumentNullException(nameof(onEnablementUpdatedCallback));
            }

            if (null == onLightLevelUpdatedCallback)
            {
                throw new ArgumentNullException(nameof(onLightLevelUpdatedCallback));
            }

            MqttConfig mqttConfig = MqttConfig.ParseConfig();
            
            MqttFactory = new MqttFactory();
            MqttClient = MqttFactory.CreateMqttClient();

            // Handle callbacks
            MqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                try
                {
                    string utfString = Encoding.UTF8.GetString(e.ApplicationMessage.Payload, 0, e.ApplicationMessage.Payload.Length);

                    if (e.ApplicationMessage.Topic.Equals($"{mqttConfig.Topic}/set", StringComparison.OrdinalIgnoreCase))
                    {
                        bool enable = Convert.ToBoolean(utfString);
                        onEnablementUpdatedCallback(enable);
                    }
                    else if (e.ApplicationMessage.Topic.Equals($"{mqttConfig.Topic}/lightlevel", StringComparison.OrdinalIgnoreCase))
                    {
                        double lightLevel = Convert.ToDouble(utfString);
                        onLightLevelUpdatedCallback(lightLevel);
                    }
                }
                catch (Exception)
                {
                }
            });

            // Handle subscription connection
            MqttClient.UseConnectedHandler(async e =>
            {
                // Subscribe to the desired topic when connected
                MqttClientSubscribeResult result = await MqttClient.SubscribeAsync(new TopicFilterBuilder()
                    .WithTopic($"{mqttConfig.Topic}/#")
                    .Build());
            });

            // Handle disconnects
            MqttClient.UseDisconnectedHandler(async e =>
            {
                // Reconnect when disconnected
                await Connect();
            });
        }

        /// <summary>
        /// Attempt to connect and subscribe to the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public async Task Connect()
        {
            MqttConfig mqttConfig = MqttConfig.ParseConfig();

            if (string.IsNullOrEmpty(mqttConfig.BrokerHostname))
            {
                throw new ArgumentNullException(nameof(mqttConfig.BrokerHostname));
            }

            if (string.IsNullOrEmpty(mqttConfig.Username))
            {
                throw new ArgumentNullException(nameof(mqttConfig.Username));
            }

            if (string.IsNullOrEmpty(mqttConfig.Password))
            {
                throw new ArgumentNullException(nameof(mqttConfig.Password));
            }

            // Create TCP-based connection options
            IMqttClientOptions mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(mqttConfig.BrokerHostname)
                .WithCredentials(mqttConfig.Username, mqttConfig.Password)
                .WithCleanSession()
                .Build();

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
        }

        /// <summary>
        /// Disconnect from the MQTT broker.
        /// </summary>
        /// <param name="entities"></param>
        public async Task Disconnect()
        {
            await MqttClient.DisconnectAsync();
        }
    }
}
