using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Q42.HueApi;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace HueController.UnitTests
{
    [TestClass]
    public class HueTests
    {
        private static readonly byte DefaultLightTestBrightness = 127;

        private static readonly int DefaultLightTestColorTemperature = LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights;

        private static readonly List<Light> DefaultLightTestGroup = new List<Light>()
        {
            new Light() {
                Id = "WhiteOnly",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.WhiteOnly],
                State = new State() { On = true, Brightness = DefaultLightTestBrightness } },

            new Light() {
                Id = "WhiteOnly-Off",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.WhiteOnly],
                State = new State() { On = false, Brightness = DefaultLightTestBrightness } },

            new Light() {
                Id = "WhiteAmbiance",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = DefaultLightTestBrightness,
                    ColorTemperature = DefaultLightTestColorTemperature } },

            new Light() {
                Id = "WhiteAmbiance-Off",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.WhiteAmbiance],
                State = new State() { On = false, Brightness = DefaultLightTestBrightness,
                    ColorTemperature = DefaultLightTestColorTemperature } },

            new Light() {
                Id = "ColorCT",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = DefaultLightTestBrightness,
                    ColorTemperature = DefaultLightTestColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorCT-Off",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.Color],
                State = new State() { On = false, Brightness = DefaultLightTestBrightness,
                    ColorTemperature = DefaultLightTestColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorXY",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = DefaultLightTestBrightness, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(DefaultLightTestColorTemperature))} },

            new Light() {
                Id = "ColorXYO-ff",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.Color],
                State = new State() { On = false, Brightness = DefaultLightTestBrightness, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(DefaultLightTestColorTemperature))} },

            new Light() {
                Id = "ColorHS",
                Type = LightDetails.LightTypeNames[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = DefaultLightTestBrightness, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(DefaultLightTestColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(DefaultLightTestColorTemperature)).S) } },
        };


        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void CalculateLightsToUpdateNoChange()
        {
            using (ShimsContext.Create())
            {
                IEnumerable<string> result = FluxCalculate.CalculateLightsToUpdate(
                    DefaultLightTestGroup,
                    DefaultLightTestColorTemperature,
                    DefaultLightTestBrightness);

                Assert.AreEqual(0, result.Count(), $"No change is expected. Lights which were updated: {String.Join(", ", result)}");
            }
        }

        [TestMethod]
        public void CalculateLightsToUpdateBrightnessOnlyChange()
        {
            using (ShimsContext.Create())
            {
                IEnumerable<string> result = FluxCalculate.CalculateLightsToUpdate(
                    DefaultLightTestGroup,
                    DefaultLightTestColorTemperature,
                    255);

                Assert.AreEqual(1, result.Count(), $"WhiteOnly is the only light expected to be changed. Lights which were updated: {String.Join(", ", result)}");
                Assert.IsTrue(result.Contains("WhiteOnly"), $"WhiteOnly light expected to be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightsToUpdateBeyondAmbianceColorTemperatureNoBrightness()
        {
            using (ShimsContext.Create())
            {
                IEnumerable<string> result = FluxCalculate.CalculateLightsToUpdate(
                    DefaultLightTestGroup, 
                    LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights + 5, 
                    null);

                Assert.AreEqual(2, result.Count(), $"Only CT and XY color lights are expected to be updated since the color temperature is above the white-ambiance threshold with no brightness shift specified. Lights which were updated: {String.Join(", ", result)}");
                Assert.IsTrue(result.Contains("ColorCT"), $"ColorCT light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorXY"), $"ColorXY light expected to be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightsToUpdateBeyondAmbianceColorTemperatureWithBrightness()
        {
            using (ShimsContext.Create())
            {
                IEnumerable<string> result = FluxCalculate.CalculateLightsToUpdate(
                    DefaultLightTestGroup,
                    LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights + 5,
                    255);

                Assert.AreEqual(3, result.Count(), $"Only White and CT and XY color lights are expected to be updated since the color temperature is above the white-ambiance threshold when a brightness shift is specified. Lights which were updated: {String.Join(", ", result)}");
                Assert.IsTrue(result.Contains("WhiteOnly"), $"WhiteOnly light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorCT"), $"ColorCT light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorXY"), $"ColorXY light expected to be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightsToUpdateWithinAmbianceColorTemperatureNoBrightness()
        {
            using (ShimsContext.Create())
            {
                IEnumerable<string> result = FluxCalculate.CalculateLightsToUpdate(
                    DefaultLightTestGroup,
                    LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights - 5,
                    null);

                Assert.AreEqual(3, result.Count(), $"Only WhiteAmbiance, CT and XY color lights are expected to be updated since the color temperature is within the white-ambiance threshold and no brightness shift was specified. Lights which were updated: {String.Join(", ", result)}");
                Assert.IsTrue(result.Contains("WhiteAmbiance"), $"WhiteAmbiance light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorCT"), $"ColorCT light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorXY"), $"ColorXY light expected to be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightsToUpdateWithinAmbianceColorTemperatureWithBrightness()
        {
            using (ShimsContext.Create())
            {
                IEnumerable<string> result = FluxCalculate.CalculateLightsToUpdate(
                    DefaultLightTestGroup,
                    LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights - 5,
                    255);

                Assert.AreEqual(4, result.Count(), $"All except HS color lights are expected to be updated since the color temperature is within the white-ambiance threshold and a brightness shift was specified. Lights which were updated: {String.Join(", ", result)}");
                Assert.IsTrue(result.Contains("WhiteOnly"), $"WhiteOnly light expected to be updated.");
                Assert.IsTrue(result.Contains("WhiteAmbiance"), $"WhiteAmbiance light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorCT"), $"ColorCT light expected to be updated.");
                Assert.IsTrue(result.Contains("ColorXY"), $"ColorXY light expected to be updated.");
            }
        }
    }
}
