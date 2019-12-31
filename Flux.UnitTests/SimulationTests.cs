//using Microsoft.QualityTools.Testing.Fakes;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Fakes;
//using System.Threading.Tasks;

//namespace HueController.UnitTests
//{
//    [TestClass]
//    public class SimulationTests
//    {
//        [TestMethod]
//        public async Task Simulate()
//        {
//            using (ShimsContext.Create())
//            {
//                ShimDateTime.NowGet = () => ShimsContext.ExecuteWithoutShims(() => DateTime.Today.AddHours(9));

//                Hue hue = new Hue();

//                hue.SetFluxConfigValues();

//                await hue.FluxUpdate();
//            }
//        }
//    }
//}
