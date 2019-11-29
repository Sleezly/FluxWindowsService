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
        private static readonly byte BrightnessDim = 1;

        private static readonly byte BrightnessMatches = 127;
        
        private static readonly int CurrentColorTemperature = LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights;

        private static readonly IReadOnlyCollection<Light> DefaultLightTestGroup = new List<Light>()
        {
            new Light() {
                Id = "WhiteOnly-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.WhiteOnly],
                State = new State() { On = true, Brightness = BrightnessDim } },

            new Light() {
                Id = "WhiteOnly-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.WhiteOnly],
                State = new State() { On = true, Brightness = BrightnessMatches } },

            new Light() {
                Id = "WhiteOnly-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.WhiteOnly],
                State = new State() { On = false } },

            new Light() {
                Id = "WhiteAmbiance-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = BrightnessDim, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Id = "WhiteAmbiance-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Id = "WhiteAmbiance-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.WhiteAmbiance],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Id = "ColorCT-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorCT-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorCT-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorXY-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Id = "ColorXY-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Id = "ColorXYO-ff",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Id = "ColorHS-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },

            new Light() {
                Id = "ColorHS-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },

            new Light() {
                Id = "ColorHS-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightDetails.LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },
        };


        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void CalculateLightCommands_NoChange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(0, result.Count(), $"No change is expected.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessChange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature,
                    BrightnessDim,
                    BrightnessMatches);

                Assert.AreEqual(1, result.Count(), $"One sets of light groups should be updated for brightness change.");
                Assert.AreEqual(5, result[BrightnessDim].Count, $"Only brightness matches lights should be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_ColorTemperatureChange_WithinWhiteAmbienceRange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature - 5,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(2, result.Count(), $"Two sets of light groups should be updated for color temperature change.");
                Assert.AreEqual(3, result[BrightnessDim].Count, $"WhiteAmbience, ColorCT and ColorXY should be updated.");
                Assert.AreEqual(3, result[BrightnessMatches].Count, $"WhiteAmbience, ColorCT and ColorXY should be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_ColorTemperatureChange_OutsideWhiteAmbienceRange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    LightDetails.MaxAllowedColorTemperatureForWhiteAmbianceLights + 5,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(2, result.Count(), $"Two sets of light groups should be updated for color temperature change.");
                Assert.AreEqual(2, result[BrightnessDim].Count, $"ColorCT and ColorXY should be updated.");
                Assert.AreEqual(2, result[BrightnessMatches].Count, $"ColorCT and ColorXY should be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_EverythingChanges()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature - 5,
                    25,
                    BrightnessMatches);

                Assert.AreEqual(2, result.Count(), $"Two sets of light groups should be updated for brightness and color temperature change.");
                Assert.AreEqual(3, result[BrightnessDim].Count, $"Lights which need color temperature updates should be updated.");
                Assert.AreEqual(5, result[25].Count, $"All should be updated.");
            }
        }
    }
}
