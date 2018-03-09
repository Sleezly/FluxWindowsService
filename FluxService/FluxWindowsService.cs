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
            this.webServer = WebApp.Start<WebSelfHostStartup>(url: baseAddress);
        }

        public void Stop()
        {
            this.webServer.Dispose();
        }
    }
}
