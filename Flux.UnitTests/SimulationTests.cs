using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace HueController.UnitTests
{
    [TestClass]
    public class SimulationTests
    {
        [TestMethod]
        public async Task Simulate()
        {
            Hue hue = new Hue(0, byte.MaxValue, 30000);
            await hue.SetFluxConfigValues();

            await hue.FluxUpdate(false, DateTime.Today.AddHours(9));
        }
    }
}
