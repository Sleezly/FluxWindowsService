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
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<string> DeserializeLightObjectGraph()
        {
            List<string> lights = new List<string>();

            using (StreamReader input = new StreamReader($"entity_registry.yaml"))
            {
                while (!input.EndOfStream)
                {
                    string name = input.ReadLine().Split('.')[1].Trim();
                    string platform = input.ReadLine().Split(':')[1].Trim();
                    string uniqueId = input.ReadLine().Split(' ')[3].Trim();

                    if (platform.Equals("hue", StringComparison.InvariantCultureIgnoreCase))
                    {
                        lights.Add(uniqueId);
                    }
                }
            }

            return lights;
        }
    }
}
