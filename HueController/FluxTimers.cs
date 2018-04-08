using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HueController
{
    [DataContract]
    public class FluxTimers
    {
        /// <summary>
        /// Logging
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Rule engine
        /// </summary>
        public List<FluxRule> Rules;

        /// <summary>
        /// Creates a new rule instance
        /// </summary>
        /// <returns></returns>
        public static FluxTimers Create(Dictionary<string, string> lightNamesToId)
        {
            FluxTimers fluxTimer = ParseConfig();

            return TranslateNamesToIds(fluxTimer, lightNamesToId);
        }

        /// <summary>
        /// Loads the JSON.
        /// </summary>
        /// <returns></returns>
        private static FluxTimers ParseConfig()
        {
            StreamReader reader = new StreamReader($"FluxTimers.json");
            string config = reader.ReadToEndAsync().Result;

            FluxTimers fluxTimers = new FluxTimers();
            fluxTimers.Rules = new List<FluxRule>();

            JEnumerable<JToken> ruleTokens = JsonConvert.DeserializeObject<JObject>(config)["Rules"].Children();
            foreach (JToken ruleToken in ruleTokens)
            {
                FluxRule rule = JsonConvert.DeserializeObject<FluxRule>(ruleToken.ToString());

                rule.LightIds = new List<string>();

                JEnumerable<JToken> lightIdTokens = JsonConvert.DeserializeObject<JObject>(ruleToken.ToString())["LightIds"].Children();

                foreach (JToken lightToken in lightIdTokens)
                {
                    rule.LightIds.Add(lightToken.ToString());
                }

                fluxTimers.Rules.Add(rule);
            }

            return fluxTimers;
        }

        /// <summary>
        /// Replaces a named list of lights with a list of ID values
        /// </summary>
        /// <param name="fluxTimer"></param>
        /// <param name="lightNamesToId"></param>
        private static FluxTimers TranslateNamesToIds(FluxTimers fluxTimer, Dictionary<string, string> lightNamesToId)
        {
            foreach (FluxRule rule in fluxTimer.Rules)
            {
                List<string> indexedList = new List<string>();

                foreach (string lightId in rule.LightIds)
                {
                    if (lightNamesToId.ContainsKey(lightId))
                    {
                        indexedList.Add(lightNamesToId[lightId]);

                        log.Info($"{nameof(TranslateNamesToIds)} Timer '{rule.Name}' has Light ID '{lightNamesToId[lightId]}' named '{lightId}'.");
                    }
                    else
                    {
                        indexedList.Add(lightId);

                        log.Info($"{nameof(TranslateNamesToIds)} Timer '{rule.Name}' has Light ID '{lightId}' which is 'unnamed'.");
                    }
                }
                rule.LightIds = indexedList;
            }

            return fluxTimer;
        }
    }

    [DataContract]
    public class FluxRule
    {
        public enum ReactWithState
        {
            Any,
            On,
            Off
        };

        public enum States
        {
            On,
            Off,
        };

        public List<string> LightIds;

        [DataMember]
        public string Name;

        [DataMember]
        public string BridgeId;

        [DataMember]
        public TimeSpan TransitionDuration;

        [DataMember]
        public TimeSpan Time;

        [DataMember]
        public Boolean Sunrise;

        [DataMember]
        public Boolean Sunset;

        [DataMember]
        public byte Brightness;

        [DataMember]
        public Boolean SetFluxColorTemperature;

        [DataMember]
        public States State;

        [DataMember]
        public Boolean OnlyReactWithPresence;

        [DataMember]
        public ReactWithState OnlyReactWithState;
    }
}