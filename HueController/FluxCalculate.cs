using Q42.HueApi;
using System;
using System.Collections.Generic;
using Utilities;

namespace HueController
{
    public static class FluxCalculate
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="lightsToScan"></param>
        /// <param name="colorTemperature"></param>
        /// <param name="transitiontime"></param>
        public static IEnumerable<string> CalculateLightsToUpdate(IEnumerable<Light> lights, int colorTemperature, byte? brightness)
        {
            // Lights to update
            List<string> lightsToUpdate = new List<string>();

            double[] currentColorTemperatureAsXY = ColorConverter.RGBtoXY(ColorConverter.MiredToRGB(colorTemperature));

            foreach (Light light in lights)
            {
                if (light.State.On)
                {
                    bool needToSetColorTemperature = false;
                    bool needToSetBrightness = brightness.HasValue ? (light.State.Brightness != brightness.Value) : false;

                    switch (light.State.ColorMode)
                    {
                        case "xy":
                            {
                                //int ct = ColorConverter.XYToTemperature(light.State.ColorCoordinates);
                                //RGB rgb = ColorConverter.MiredToRGB(ct);
                                //double[] xy = ColorConverter.RGBtoXY(rgb);

                                //if (Math.Abs(xy[0] - currentColorTemperatureAsXY[0]) + Math.Abs(xy[1] - currentColorTemperatureAsXY[1]) < 0.15)
                                //ColorConverter.XYToTemperature(currentColorTemperatureAsXY) != ColorConverter.XYToTemperature(light.State.ColorCoordinates))

                                // Sum of the X and Y color difference must be within a reasonable percentage to be considered a match. This
                                // ensures color changes are not overridden by Flux but also allows for XY color values which are still
                                // in the color temperature spectrum to be Flux-controlled.

                                double xyDifference = Math.Abs(light.State.ColorCoordinates[0] - currentColorTemperatureAsXY[0]) + Math.Abs(light.State.ColorCoordinates[1] - currentColorTemperatureAsXY[1]);
                                if (xyDifference > 0.001 && xyDifference < 0.15)
                                {
                                    needToSetColorTemperature = true;
                                }
                            }
                            break;

                        case "hs":
                            // Hue & Saturation is always to be ignored by Flux.
                            needToSetColorTemperature = false;
                            break;

                        case "ct":
                        default:
                            // Only set ColorTemp values if there is a change.
                            needToSetColorTemperature = (light.State.ColorTemperature != colorTemperature);
                            break;
                    }

                    LightDetails.LightType lightType = LightDetails.TranslateStringToLightType(light.Type);

                    if (lightType == LightDetails.LightType.WhiteOnly)
                    {
                        if (needToSetBrightness)
                        {
                            lightsToUpdate.Add(light.Id);
                        }
                    }
                    else
                    {
                        // don't adjust color temperature outside of allowed temperature threshold
                        if (needToSetColorTemperature && LightDetails.IsInAllowedColorRange(lightType, colorTemperature))
                        {
                            lightsToUpdate.Add(light.Id);
                            //log.Debug($"'{nameof(ModifyFluxLights)}' found '{light.Id}', '{light.Name}' with temperature '{light.State.ColorTemperature}' and brightness '{light.State.Brightness}'.");
                        }
                    }
                }
            }

            return lightsToUpdate;
        }
    }
}
