using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace HueController.UnitTests
{
    [TestClass]
    public class FluxTests
    {
        private Flux flux = new Flux()
        {
            Latitude = 47.8601,
            Longitude = -122.2043,

            StopTime = TimeSpan.FromHours(22),

            SunriseColorTemperature = 3400,
            SolarNoonTemperature = 5000,
            SunsetColorTemperature = 2400,
            StopColorTemperature = 2000,
        };

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void TestCreateHue()
        {
            Hue hue = Hue.GetOrCreate();
        }

        [TestMethod]
        public void TestLightEntityRegistry()
        {
            LightEntityRegistry.DeserializeLightObjectGraph();
        }

        [TestMethod]
        public void TestGetPollingRate()
        {
            flux.Today = new DateTime(2018, 1, 1);

            TimeSpan timeToSleep = flux.GetThreadSleepDuration(new DateTime(2018, 1, 1, 5, 0, 0));
            timeToSleep = flux.GetThreadSleepDuration(new DateTime(2018, 1, 1, 6, 59, 59));
            timeToSleep = flux.GetThreadSleepDuration(new DateTime(2018, 1, 1, 7, 30, 0));
            timeToSleep = flux.GetThreadSleepDuration(new DateTime(2018, 1, 1, 10, 0, 0));
            timeToSleep = flux.GetThreadSleepDuration(new DateTime(2018, 1, 1, 19, 0, 0));
            timeToSleep = flux.GetThreadSleepDuration(new DateTime(2018, 1, 1, 21, 0, 0));
        }

        [TestMethod]
        public void TestSimulateFiveDays()
        {
            // override default behavior of Today
            flux.Today = new DateTime(2018, 2, 22);

            DateTime time = flux.Today + TimeSpan.FromSeconds(1);
            DateTime stopTime = flux.Today + TimeSpan.FromDays(15);

            int colorTemperaturePrevious = -1;
            int steps = 0;

            while (time < stopTime)
            {
                TimeSpan timeToSleep = flux.GetThreadSleepDuration(time);
                Assert.IsTrue(timeToSleep.TotalSeconds > 1);

                int colorTemperatureCurrent = flux.GetColorTemperature(time + timeToSleep);
                Assert.IsTrue(colorTemperaturePrevious != colorTemperatureCurrent);

                time += timeToSleep;

                Debug.WriteLine($"{time}, {colorTemperatureCurrent}, '{timeToSleep}'");

                colorTemperaturePrevious = colorTemperatureCurrent;
                ++steps;
            }
        }

        [TestMethod]
        public void TestSimulateToday()
        {
            DateTime time = DateTime.Today + TimeSpan.FromSeconds(1);
            DateTime stopTime = DateTime.Today + new TimeSpan(23, 59, 59);

            int colorTemperaturePrevious = -1;
            int steps = 0;

            while (time < stopTime)
            {
                TimeSpan timeToSleep = flux.GetThreadSleepDuration(time);
                //Assert.IsTrue(timeToSleep.TotalSeconds > 1);

                int colorTemperatureCurrent = flux.GetColorTemperature(time + timeToSleep);
                Assert.IsTrue(colorTemperaturePrevious != colorTemperatureCurrent);

                time += timeToSleep;

                Debug.WriteLine($"{time}, {colorTemperatureCurrent}, '{timeToSleep}'");
                //Debug.WriteLine($"{time}, {colorTemperatureCurrent}");
                //Debug.WriteLine($"{timeToSleep.TotalSeconds}, {time}");
                //Debug.WriteLine($"{timeToSleep.TotalSeconds}, {colorTemperatureCurrent}");
                //Debug.WriteLine($"{timeToSleep.TotalSeconds}");
                //Debug.WriteLine($"{colorTemperatureCurrent}");

                colorTemperaturePrevious = colorTemperatureCurrent;
                ++steps;
            }
        }

        [TestMethod]
        public void TestGetColorTemperatureCompleteDay()
        {
            DateTime time = DateTime.Today + TimeSpan.FromMinutes(1);
            DateTime stopTime = DateTime.Today + new TimeSpan(23, 59, 0);

            while (time < stopTime)
            {
                TimeSpan timeToSleep = flux.GetThreadSleepDuration(time);

                time += timeToSleep;

                int colorTemperatureCurrent = flux.GetColorTemperature(time);

                while (timeToSleep.TotalMinutes > 0)
                {
                    Debug.WriteLine($"{time}, {colorTemperatureCurrent}, '{timeToSleep}'");

                    timeToSleep -= TimeSpan.FromMinutes(1);
                }
            }
        }

        [TestMethod]
        public void TestGetColorTemperatureSingle()
        {
            const int endDuration = 100;

            for (int i = 0; i < endDuration; i++)
            {
                int value = Flux.GetColorTemperature(500, 0, i, endDuration);

                Debug.WriteLine($"{value}");
            }
        }
    }
}
