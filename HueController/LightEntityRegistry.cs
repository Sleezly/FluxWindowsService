using System;
using System.Collections.Generic;
using System.IO;

namespace HueController
{
    public class LightEntityRegistry
    {
        private const string LightEntityRegistryFilename = "LightEntityRegistry.yaml";

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

            if (File.Exists(LightEntityRegistryFilename))
            {
                using (StreamReader input = new StreamReader(LightEntityRegistryFilename))
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
                    log.Info($"Found no lights to translate any lights in {nameof(DeserializeLightObjectGraph)}");
                }
            }

            return lights;
        }
    }
}
