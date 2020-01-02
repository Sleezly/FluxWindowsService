using HueController;
using Microsoft.Owin.Hosting;
using System;
using System.Threading.Tasks;

namespace FluxService
{
    public class FluxWindowsService
    {
        /// <summary>
        /// Hue instance.
        /// </summary>
        public readonly static Hue Hue = new Hue();

        /// <summary>
        /// Web server instance.
        /// </summary>
        private IDisposable webServer;

        /// <summary>
        /// Web server port.
        /// </summary>
        private const string baseAddress = "http://+:51234/";

        /// <summary>
        /// MQTT subscriber.
        /// </summary>
        private readonly MqttSubscriber mqttSubscriber = new MqttSubscriber(OnEnablementUpdatedCallback, OnLightLevelUpdatedCallback);

        /// <summary>
        /// Starting point.
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            // Connect to the MQTT broker.
            await mqttSubscriber.Connect();

            //
            // To enable self-hosting of a restful endpint, be sure to run this command from an elevated command prompt:
            // $> netsh http add urlacl url=http://+:51234/ user=Everyone
            //
            // To view all urlacls currently active, run:
            // $> netsh http show urlacl
            //
            // If the above is not executed, starting the web app below will fail.
            //
            // Also be sure to open port the broadcast port through windows firewall to allow incoming traffic in 'private', assuming
            // your network profile is set to private. If this doesn't work, double-check profile is set to private rather than public.
            //

            this.webServer = WebApp.Start<WebSelfHostStartup>(url: baseAddress);
        }

        /// <summary>
        /// Stops the web server.
        /// </summary>
        public void Stop()
        {
            this.webServer.Dispose();
        }

        /// <summary>
        /// Handles enablement changes via MQTT subscription.
        /// </summary>
        /// <param name="enabled"></param>
        private static async Task OnEnablementUpdatedCallback(bool enabled)
        {
            await FluxWindowsService.Hue.Enable(enabled);
        }

        /// <summary>
        /// Handles light level changes via MQTT subscription.
        /// </summary>
        private static void OnLightLevelUpdatedCallback(double lightLevel)
        {
            FluxWindowsService.Hue.LightLevel = lightLevel;
        }
    }
}
