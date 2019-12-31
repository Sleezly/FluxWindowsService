using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HueController.UnitTests
{
    [TestClass]
    public class HueTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void HueGetStatus()
        {
            Hue hue = new Hue();
            hue.GetStatus();
        }

        [TestMethod]
        public void HueLightEntityRegistry()
        {
            LightConfig.DeserializeLightObjectGraph();
        }
    }
}
