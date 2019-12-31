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
        private static readonly byte BrightnessDim = 8;

        private static readonly byte BrightnessMatches = 128;
        
        private static readonly Primitives.ColorTemperature CurrentColorTemperature = ColorTemperatureExtensions.MaxAllowedColorTemperatureForWhiteAmbianceLights;

        private static readonly IReadOnlyCollection<Light> DefaultLightTestGroup = new List<Light>()
        {
            new Light() {
                Name = "WhiteOnly-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteOnly],
                State = new State() { On = true, Brightness = BrightnessDim } },

            new Light() {
                Name = "WhiteOnly-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteOnly],
                State = new State() { On = true, Brightness = BrightnessMatches } },

            new Light() {
                Name = "WhiteOnly-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteOnly],
                State = new State() { On = false } },

            new Light() {
                Name = "WhiteAmbiance-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = BrightnessDim, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Name = "WhiteAmbiance-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Name = "WhiteAmbiance-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Name = "ColorCT-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Name = "ColorCT-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Name = "ColorCT-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Name = "ColorXY-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Name = "ColorXY-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Name = "ColorXYO-ff",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Name = "ColorHS-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },

            new Light() {
                Name = "ColorHS-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },

            new Light() {
                Name = "ColorHS-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },
        };

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void CalculateLightCommands_AllLightStyles_NoChange()
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
        public void CalculateLightCommands_AllLightStyles_BrightnessChange()
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
        public void CalculateLightCommands_AllLightStyles_ColorTemperatureChange_WithinWhiteAmbienceRange()
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
        public void CalculateLightCommands_AllLightStyles_ColorTemperatureChange_OutsideWhiteAmbienceRange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    ColorTemperatureExtensions.MaxAllowedColorTemperatureForWhiteAmbianceLights + 5,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(2, result.Count(), $"Two sets of light groups should be updated for color temperature change.");
                Assert.AreEqual(2, result[BrightnessDim].Count, $"ColorCT and ColorXY should be updated.");
                Assert.AreEqual(2, result[BrightnessMatches].Count, $"ColorCT and ColorXY should be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_AllLightStyles_EverythingChanges()
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

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_0()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    new List<Light>()
                    {
                        new Light() {
                            Name = "Test 6",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = 0, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 4",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)(BrightnessMatches - 4), ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 5",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = 0, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 3",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)(BrightnessMatches + 2), ColorTemperature = CurrentColorTemperature } },
                    },
                    CurrentColorTemperature,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.IsTrue(result.ContainsKey((byte)(BrightnessMatches + 2)), $"The single light group should match the higest valued brightness byte.");
                Assert.AreEqual(3, result[(byte)(BrightnessMatches + 2)].Count, $"The three lights which don't match the most common brightness should be adjusted.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_1()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    new List<Light>()
                    {
                        new Light() {
                            Name = "Test 4",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)(BrightnessMatches + 2), ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 5",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = 0, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 2",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 3",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)(BrightnessMatches - 4), ColorTemperature = CurrentColorTemperature } },
                    },
                    CurrentColorTemperature,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.IsTrue(result.ContainsKey(BrightnessMatches), $"The single light group should be the most common name.");
                Assert.AreEqual(3, result[BrightnessMatches].Count, $"The three lights which don't match the most common brightness should be adjusted.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_2()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    new List<Light>()
                    {
                        new Light() {
                            Name = "Test 4",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)(BrightnessMatches + 2), ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 1",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 5",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = 0, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 2",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 3",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)(BrightnessMatches - 4), ColorTemperature = CurrentColorTemperature } },
                    },
                    CurrentColorTemperature,
                    BrightnessMatches,
                    BrightnessMatches);

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.IsTrue(result.ContainsKey(BrightnessMatches), $"The single light group should be the most common name.");
                Assert.AreEqual(3, result[BrightnessMatches].Count, $"The three lights which don't match the most common brightness should be adjusted.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_GroupHasMismatch()
        {
            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    new List<Light>()
                    {
                        new Light() {
                            Name = "Test 1",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = 145, ColorTemperature = CurrentColorTemperature } },

                        new Light() {
                            Name = "Test 2",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = 147, ColorTemperature = CurrentColorTemperature } },                    },
                    CurrentColorTemperature,
                    146,
                    145);

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.IsTrue(result.ContainsKey(146), $"Lights should be adjusted to the new brightness.");
            }
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(128)]
        [DataRow(129)]
        [DataRow(246)]
        [DataRow(247)]
        [DataRow(248)]
        [DataRow(254)]
        [DataRow(255)]
        public void CalculateLightCommands_Brightness_Single(int currentBrightness)
        {
            const byte newBrightness = 128;

            using (ShimsContext.Create())
            {
                Dictionary<byte, List<string>> result = Hue.CalculateLightCommands(
                    new List<Light>()
                    {
                        new Light() {
                            Name = "Test",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)currentBrightness, ColorTemperature = CurrentColorTemperature } },
                    },
                    CurrentColorTemperature,
                    newBrightness,
                    newBrightness);

                bool brightnessChangeExpected = (currentBrightness == 0 || currentBrightness >= 247) && currentBrightness != newBrightness;

                Assert.AreEqual(brightnessChangeExpected ? 1 : 0, result.Count(), $"One light group expected since change is expected.");

                if (brightnessChangeExpected)
                {
                    Assert.AreEqual(newBrightness, result.First().Key, $"Brightness level should be new level.");
                    Assert.AreEqual(1, result.First().Value.Count, $"1 light in group expected.");
                }
            }
        }
    }
}
