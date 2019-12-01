using HueController.Fakes;using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Fakes;
using System.Threading.Tasks;

namespace HueController.UnitTests
{
    [TestClass]
    public class SimulationTests
    {
        [TestMethod]
        public async Task Simulate()
        {
            using (ShimsContext.Create())
            {
                ShimDateTime.NowGet = () => ShimsContext.ExecuteWithoutShims(() => DateTime.Today.AddHours(9));
                ShimHue.AllInstances.ConnectClientDictionaryOfStringString = (x, bridgeIds) =>
                {
                    return Task.FromResult(new List<KeyValuePair<HueBridge, List<LightDetails>>>());
                };

                Hue hue = new Hue();

                await hue.SetFluxConfigValues();

                await hue.FluxUpdate();
            }
        }
    }
}
