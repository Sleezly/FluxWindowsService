using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;

namespace FluxService
{
    [DataContract]
    public class MqttConfig
    {
        /// <summary>
        /// MQTT Broker Hostname.
        /// </summary>
        [DataMember]
        public string BrokerHostname;

        /// <summary>
        /// MQTT Subscription Topic.
        /// </summary>
        [DataMember]
        public string Topic;

        /// <summary>
        /// MQTT User Name.
        /// </summary>
        [DataMember]
        public string Username;

        /// <summary>
        /// HQTT User Password.
        /// </summary>
        [DataMember]
        public string Password;

        /// <summary>
        /// JSON filename.
        /// </summary>
        private static readonly string JsonFilename = @"Config\MqttConfig.json";

        /// <summary>
        /// Constructor.
        /// </summary>
        private MqttConfig()
        {
        }

        /// <summary>
        /// Load the configuration file.
        /// </summary>
        /// <returns></returns>
        public static MqttConfig ParseConfig()
        {
            StreamReader reader = new StreamReader(JsonFilename);
            string config = reader.ReadToEndAsync().GetAwaiter().GetResult();
            return JsonConvert.DeserializeObject<MqttConfig>(config);
        }
    }
}
