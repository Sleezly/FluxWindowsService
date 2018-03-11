using Microsoft.Owin.Hosting;
using System;

namespace FluxService
{
    public class FluxWindowsService
    {
        private IDisposable webServer;

        private const string baseAddress = "http://+:51234/";

        public void Start()
        {
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

        public void Stop()
        {
            this.webServer.Dispose();
        }
    }
}
