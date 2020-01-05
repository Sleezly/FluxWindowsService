using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Q42.HueApi;
using Q42.HueApi.Fakes;
using Q42.HueApi.Models;
using Q42.HueApi.Models.Groups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace HueController.UnitTests
{
    [TestClass]
    public class HueModifyScenesTests
    {
        private void SetupDefaultShims(IEnumerable<Light> lights)
        {
            ShimLocalHueClient.ConstructorString = (client, ip) => { };

            ShimHueClient.AllInstances.GetScenesAsync = (client) =>
            {
                IReadOnlyCollection<Scene> scenes = new List<Scene>
                {
                    new Scene()
                    {
                        Id = "Flux Scene 1",
                        Name = "Flux Scene 1",
                    },
                };

                return Task.FromResult(scenes);
            };

            ShimHueClient.AllInstances.GetSceneAsyncString = (client, sceneId) =>
            {
                return Task.FromResult(new Scene()
                {
                    Id = sceneId,
                    Name = sceneId,
                    LightStates = lights.ToDictionary(light => light.Id, light => light.State),
                });
            };

            ShimHueClient.AllInstances.ModifySceneAsyncStringStringLightCommand = (client, sceneId, lightId, lightComment) =>
            {
                return Task.FromResult(new HueResults());
            };
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public async Task HueModifyFluxSwitchScenes()
        {
            const byte brightnessDim = 5;
            const byte brightnessPrevious = 200;
            const byte brightnessNew = 225;
            Primitives.ColorTemperature colorTemperature = 400;

            using (ShimsContext.Create())
            {
                IEnumerable<Light> lights = new List<Light>()
                {
                    new Light() {
                        Id = "Test 1",
                        Name = "Test 1",
                        Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                        State = new State() { On = true, Brightness = brightnessDim, ColorTemperature = colorTemperature } },
                    new Light() {
                        Id = "Test 2",
                        Name = "Test 2",
                        Type = LightExtensions.LightTypeToNameMapping[LightType.WhiteAmbiance],
                        State = new State() { On = true, Brightness = brightnessPrevious, ColorTemperature = colorTemperature } },
                };

                SetupDefaultShims(lights);

                HueClient hueClient = new LocalHueClient(null);

                Hue hue = new Hue(null);

                hue.UpdateFluxStatus(brightnessPrevious, colorTemperature);

                await hue.ModifyFluxSwitchScenes(hueClient, lights, colorTemperature, brightnessNew);
            }
        }
    }
}
