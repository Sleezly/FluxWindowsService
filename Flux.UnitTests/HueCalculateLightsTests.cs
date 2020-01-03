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
    public class HueCalculateLightsTests
    {
        private static readonly byte BrightnessDim = 8;

        private static readonly byte BrightnessMatches = 128;

        private static readonly TimeSpan TransitionTime = TimeSpan.FromSeconds(1);
        
        private static readonly Primitives.ColorTemperature CurrentColorTemperature = ColorTemperatureExtensions.MaxAllowedColorTemperatureForWhiteAmbianceLights;

        private static readonly IReadOnlyCollection<Light> DefaultLightTestGroup = new List<Light>()
        {
            new Light() {
                Id = "WhiteOnly-Dim",
                Name = "WhiteOnly-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteOnly],
                State = new State() { On = true, Brightness = BrightnessDim } },

            new Light() {
                Id = "WhiteOnly-Matches",
                Name = "WhiteOnly-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteOnly],
                State = new State() { On = true, Brightness = BrightnessMatches } },

            new Light() {
                Id = "WhiteOnly-Off",
                Name = "WhiteOnly-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteOnly],
                State = new State() { On = false } },

            new Light() {
                Id = "WhiteAmbiance-Dim",
                Name = "WhiteAmbiance-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = BrightnessDim, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Id = "WhiteAmbiance-Matches",
                Name = "WhiteAmbiance-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Id = "WhiteAmbiance-Off",
                Name = "WhiteAmbiance-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature } },

            new Light() {
                Id = "ColorCT-Dim",
                Name = "ColorCT-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorCT-Matches",
                Name = "ColorCT-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorCT-Off",
                Name = "ColorCT-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorTemperature = CurrentColorTemperature, ColorMode = "ct" } },

            new Light() {
                Id = "ColorXY-Dim",
                Name = "ColorXY-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Id = "ColorXY-Matches",
                Name = "ColorXY-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Id = "ColorXY-Off",
                Name = "ColorXY-Off",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = false, Brightness = BrightnessMatches, ColorMode = "xy",
                    ColorCoordinates = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(CurrentColorTemperature))} },

            new Light() {
                Id = "ColorHS-Dim",
                Name = "ColorHS-Dim",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessDim, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },

            new Light() {
                Id = "ColorHS-Matches",
                Name = "ColorHS-Matches",
                Type = LightExtensions.LightTypeToNameMapping[LightType.Color],
                State = new State() { On = true, Brightness = BrightnessMatches, ColorMode = "hs",
                    Hue = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).H),
                    Saturation = Convert.ToInt32(ColorConverter.RGBtoHSV(ColorConverter.MiredToRGB(CurrentColorTemperature)).S) } },

            new Light() {
                Id = "ColorHS-Off",
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
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature,
                    BrightnessMatches,
                    BrightnessMatches,
                    TransitionTime);

                Assert.AreEqual(0, result.Count(), $"No change is expected.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_AllLightStyles_BrightnessChange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature,
                    BrightnessDim,
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.Brightness == BrightnessDim);
                Assert.IsNotNull(lightCommandKey);

                Assert.AreEqual(1, result.Count(), $"One sets of light groups should be updated for brightness change.");
                Assert.AreEqual(5, result[lightCommandKey].Count, $"Only brightness matches lights should be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_AllLightStyles_ColorTemperatureChange_WithinWhiteAmbienceRange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature - 5,
                    BrightnessMatches,
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.ColorTemperature == CurrentColorTemperature - 5);
                Assert.IsNotNull(lightCommandKey);

                Assert.AreEqual(1, result.Count(), $"Brightness change not expected so all lights should be updated for color temperature change.");
                Assert.AreEqual(6, result[lightCommandKey].Count, $"All lights to be updated.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_AllLightStyles_ColorTemperatureChange_OutsideWhiteAmbienceRange()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    ColorTemperatureExtensions.MaxAllowedColorTemperatureForWhiteAmbianceLights + 5,
                    BrightnessMatches,
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.ColorTemperature == ColorTemperatureExtensions.MaxAllowedColorTemperatureForWhiteAmbianceLights + 5);
                Assert.IsNotNull(lightCommandKey);

                Assert.AreEqual(1, result.Count(), $"Brightness change not expected so all lights should be updated for color temperature change.");
                Assert.AreEqual(4, result[lightCommandKey].Count, $"Color lights to be updated. White Ambiance lights to be excluded.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_AllLightStyles_EverythingChanges()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
                    DefaultLightTestGroup,
                    CurrentColorTemperature - 5,
                    25,
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey1 = result.Keys.SingleOrDefault(x => x.Brightness == 25 && x.ColorTemperature == null);
                LightCommand lightCommandKey2 = result.Keys.SingleOrDefault(x => x.Brightness == null && x.ColorTemperature == CurrentColorTemperature - 5);
                LightCommand lightCommandKey3 = result.Keys.SingleOrDefault(x => x.Brightness == 25 && x.ColorTemperature == CurrentColorTemperature - 5);

                Assert.IsNotNull(lightCommandKey1);
                Assert.IsNotNull(lightCommandKey2);
                Assert.IsNotNull(lightCommandKey3);

                Assert.AreEqual(3, result.Count(), $"Three light groups to update. ColorTemp only, Brightness and ColorTemp, and Brightness only.");
                Assert.AreEqual(2, result[lightCommandKey1].Count, $"Brightness Only.");
                Assert.AreEqual(3, result[lightCommandKey2].Count, $"ColorTemp Only.");
                Assert.AreEqual(3, result[lightCommandKey3].Count, $"Brightness and ColorTemp.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_0()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
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
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.Brightness == BrightnessMatches + 2 && x.ColorTemperature == null);
                Assert.IsNotNull(lightCommandKey, "Brightness for a group is rounded to the highest common value.");

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.AreEqual(3, result[lightCommandKey].Count, $"The three lights which don't match the most common brightness should be adjusted.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_1()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
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
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.Brightness == BrightnessMatches && x.ColorTemperature == null);
                Assert.IsNotNull(lightCommandKey, "Brightness for a group is rounded to the highest common value.");

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.AreEqual(3, result[lightCommandKey].Count, $"The three lights which don't match the most common brightness should be adjusted.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_2()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
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
                    BrightnessMatches,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.Brightness == BrightnessMatches && x.ColorTemperature == null);
                Assert.IsNotNull(lightCommandKey, "Brightness for a group is rounded to the highest common value.");

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.AreEqual(3, result[lightCommandKey].Count, $"The three lights which don't match the most common brightness should be adjusted.");
            }
        }

        [TestMethod]
        public void CalculateLightCommands_BrightnessGroupMatch_GroupHasMismatch()
        {
            using (ShimsContext.Create())
            {
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
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
                    145,
                    TransitionTime);

                LightCommand lightCommandKey = result.Keys.SingleOrDefault(x => x.Brightness == 146 && x.ColorTemperature == null);
                Assert.IsNotNull(lightCommandKey, "Brightness for a group is rounded to the highest common value.");

                Assert.AreEqual(1, result.Count(), $"One light group expected since all lights share the same common name.");
                Assert.AreEqual(2, result[lightCommandKey].Count, $"Both lights should be adjusted.");
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
                Dictionary<LightCommand, IList<string>> result = Hue.CalculateLightCommands(
                    new List<Light>()
                    {
                        new Light() {
                            Name = "Test",
                            Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                            State = new State() { On = true, Brightness = (byte)currentBrightness, ColorTemperature = CurrentColorTemperature } },
                    },
                    CurrentColorTemperature,
                    newBrightness,
                    newBrightness,
                    TransitionTime);

                bool brightnessChangeExpected = (currentBrightness == 0 || currentBrightness >= 247) && currentBrightness != newBrightness;

                Assert.AreEqual(brightnessChangeExpected ? 1 : 0, result.Count(), $"One light group expected since change is expected.");

                if (brightnessChangeExpected)
                {
                    Assert.AreEqual(newBrightness, result.First().Key.Brightness, $"Brightness level should be new level.");
                    Assert.AreEqual(1, result.First().Value.Count, $"1 light in group expected.");
                }
            }
        }
    }
}
