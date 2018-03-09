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
            Task.Run(() => SomethingHappened());
        }

        public void SomethingHappened()
        {
            while (true)
            {
                HueDetails status = FluxService.GetHueData();

                this.BeginInvoke(new MethodInvoker(delegate
                {

                    string infoLabels = "";
                    string infoData = "";

                    infoLabels += "\nStatus:";
                    infoLabels += "\n";
                    infoLabels += "\nSunrise:";
                    infoLabels += "\nSolar Noon:";
                    infoLabels += "\nSunset:";
                    infoLabels += "\nFlux Stop:";
                    infoLabels += "\n";
                    infoLabels += "\nSleeping:";
                    infoLabels += "\nWake at:";

                    infoData += "\n" + (status.On ? "On" : "Off");
                    infoData += "\n";
                    infoData += "\n" + status.FluxStatus.Sunrise.ToShortTimeString();
                    infoData += "\n" + status.FluxStatus.SolarNoon.ToShortTimeString();
                    infoData += "\n" + status.FluxStatus.Sunset.ToShortTimeString();
                    infoData += "\n" + status.FluxStatus.StopTime.ToShortTimeString();
                    infoData += "\n";
                    infoData += "\n" + Math.Round(status.CurrentSleepDuration.TotalMinutes, 2, MidpointRounding.AwayFromZero) + " minutes";
                    infoData += "\n" + status.CurrentWakeCycle.ToShortTimeString();

                    labelInfo.Text = infoLabels;
                    labelData.Text = infoData;

                    labelCurrentFluxTemperature.Text = status.FluxStatus.FluxColorTemperature.ToString();
                }));

                TimeSpan timeToSleep = status.CurrentWakeCycle - DateTime.Now;

                if (timeToSleep > TimeSpan.FromSeconds(1))
                {
                    Thread.Sleep(timeToSleep);
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                }
            }
        }

        private string TodayOrTomorrow(DateTime date)
        {
            return (date.Day == DateTime.Today.Day) ? "Today" : "Tomorrow";
        }
    }
}
