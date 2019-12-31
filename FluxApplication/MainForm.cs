using Fluxer;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HueController
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Task.Run(() => WorkerThreadAsync());
        }

        public async Task WorkerThreadAsync()
        {
            DateTime lastUpdate = default(DateTime);

            while (true)
            {
                HueStatus hueStatus = await FluxService.GetHueData();

                this.BeginInvoke(new MethodInvoker(delegate
                {
                    string infoLabels = "";
                    string infoData = "";

                    infoLabels += "\nBrightness:";
                    infoLabels += "\nLightlevel:";
                    infoLabels += "\n";
                    infoLabels += "\nStatus:";
                    infoLabels += "\n";
                    infoLabels += "\nSunrise:";
                    infoLabels += "\nSolar Noon:";
                    infoLabels += "\nSunset:";
                    infoLabels += "\nFlux Stop:";
                    infoLabels += "\n";
                    infoLabels += "\nLast Update:";
                    infoLabels += "\n";
                    infoLabels += "\nSleeping:";
                    infoLabels += "\nWake at:";

                    infoData += "\n" + hueStatus.LastBrightness.ToString();
                    infoData += "\n" + hueStatus.LastLightlevel?.ToString("N0");
                    infoData += "\n";
                    infoData += "\n" + (hueStatus.On ? "On" : "Off");
                    infoData += "\n";
                    infoData += "\n" + hueStatus.FluxStatus.Sunrise.ToShortTimeString();
                    infoData += "\n" + hueStatus.FluxStatus.SolarNoon.ToShortTimeString();
                    infoData += "\n" + hueStatus.FluxStatus.Sunset.ToShortTimeString();
                    infoData += "\n" + hueStatus.FluxStatus.StopTime.ToShortTimeString();
                    infoData += "\n";
                    infoData += "\n" + (lastUpdate == default ? string.Empty : lastUpdate.ToShortTimeString());
                    infoData += "\n";
                    infoData += "\n" + hueStatus.CurrentSleepDuration?.ToString("c");
                    infoData += "\n" + hueStatus.CurrentWakeCycle?.ToShortTimeString();

                    labelInfo.Text = infoLabels;
                    labelData.Text = infoData;

                    labelCurrentFluxTemperature.Text = hueStatus.LastColorTemperature.ToString();

                    lastUpdate = hueStatus.CurrentWakeCycle ?? default;
                }));

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}
