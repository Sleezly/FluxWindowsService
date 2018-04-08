using System;
using System.Collections.Generic;
using System.IO;

namespace HueController
{
    public class LightEntityRegistry
    {
        public string Platform { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Logging
        /// </summary>
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> DeserializeLightObjectGraph()
        {
            List<string> lights = new List<string>();

            using (StreamReader input = new StreamReader($"LightEntityRegistry.yaml"))
            {
                while (!input.EndOfStream)
                {
                    string currentLine = input.ReadLine();

                    if (currentLine.StartsWith("light.") && currentLine.EndsWith(":"))
                    {
                        string name = currentLine.Split('.')[1].Split(':')[0].Trim();

                        lights.Add(name.Replace('_', ' '));
                    }
                }
            }

            if (lights.Count == 0)
            {
                throw new Exception($"Failed to translate any lights in {nameof(DeserializeLightObjectGraph)}");
            }

            //lights.Sort();
            //log.Info($"{nameof(DeserializeLightObjectGraph)} Lights to be Flux controller:\n  {String.Join("\n  ", lights)}");

            return lights;
        }
    }
}
