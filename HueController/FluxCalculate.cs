using HueController.Primitives;
using Innovative.SolarCalculator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HueController
{
    public class FluxCalculate
    {
        public readonly double Longitude;
        public readonly double Latitude;

        public readonly int StopColorTemperature;
        public readonly int SunriseColorTemperature;
        public readonly int SunsetColorTemperature;
        public readonly int SolarNoonTemperature;

        public readonly TimeSpan StopTime;

        public DateTime DateForCircadian => new DateTime(2020, 06, 01, 0, 0, 0, 0);

        public DateTime StopDate { get { return DateForCircadian + StopTime; } }
        public DateTime Sunrise { get { return GetSunrise(DateForCircadian); } }
        public DateTime Sunset { get { return GetSunset(DateForCircadian); } }
        public DateTime SolarNoon { get { return GetSolarNoon(DateForCircadian); } }

        public FluxCalculate()
        {
            FluxConfig fluxConfig = FluxConfig.ParseConfig();

            Latitude = fluxConfig.Latitude;
            Longitude = fluxConfig.Longitude;
            StopTime = fluxConfig.StopTime;
            SolarNoonTemperature = fluxConfig.SolarNoonTemperature;
            StopColorTemperature = fluxConfig.StopColorTemperature;
            SunriseColorTemperature = fluxConfig.SunriseColorTemperature;
            SunsetColorTemperature = fluxConfig.SunsetColorTemperature;
        }

        public FluxStatus GetStatus()
        {
            return new FluxStatus()
            {
                FluxColorTemperature = GetColorTemperature(DateTime.Now),

                StopColorTemperature = StopColorTemperature,
                SunriseColorTemperature = SunriseColorTemperature,
                SunsetColorTemperature = SunsetColorTemperature,
                SolarNoonTemperature = SolarNoonTemperature,

                StopTime = StopDate,
                Sunrise = Sunrise,
                Sunset = Sunset,
                SolarNoon = SolarNoon,
            };
        }

        /// <summary>
        /// Converts a Kelvin value to a color temperature.
        /// </summary>
        /// <param name="kelvin"></param>
        /// <returns></returns>
        public static int ConvertKelvinToColorTemperature(double kelvin)
        {
            return Convert.ToInt32(Math.Floor(1000000.0 / kelvin));
        }

        /// <summary>
        /// Gets time of Sunrise for today.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public DateTime GetSunrise(DateTime now)
        {
            SolarTimes solarTimes = new SolarTimes(now.Date, Latitude, Longitude);
            return solarTimes.Sunrise;
        }

        /// <summary>
        /// Gets time of Sunset for today.
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public DateTime GetSunset(DateTime now)
        {
            SolarTimes solarTimes = new SolarTimes(now.Date, Latitude, Longitude);
            return solarTimes.Sunset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public DateTime GetSolarNoon(DateTime now)
        {
            SolarTimes solarTimes = new SolarTimes(now.Date, Latitude, Longitude);
            return solarTimes.SolarNoon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public TimeSpan GetThreadSleepDuration(DateTime now)
        {
            List<KeyValuePair<DateTime, int>> pairs = GetSortedTimesAndColorTemperaturePairs(now);

            TimeSpan currentTimeSpan = new TimeSpan(
                0,
                now.Hour,
                now.Minute,
                now.Second);

            TimeSpan startingTimeSpan = new TimeSpan(
                0,
                pairs[0].Key.Hour,
                pairs[0].Key.Minute,
                pairs[0].Key.Second);

            TimeSpan endingTimeSpan = new TimeSpan(
                pairs[0].Key.Day == pairs[1].Key.Day ? 0 : 1, // when time spans multiple days handle in a month/year agnostic manner
                pairs[1].Key.Hour,
                pairs[1].Key.Minute,
                pairs[1].Key.Second);

            if (startingTimeSpan == endingTimeSpan)
            {
                throw new ArgumentException($"Thw two provided time values of '{startingTimeSpan.ToString()}' should not match.");
            }

            if (pairs[0].Key > pairs[1].Key)
            {
                throw new ArgumentException($"Invalid provided TimeSpan values. Start time should be less than end time.");
            }

            TimeSpan sleepDuration;

            if (pairs[0].Value == pairs[1].Value)
            {
                sleepDuration = pairs[1].Key - now;
                sleepDuration += TimeSpan.FromMilliseconds(1000 - sleepDuration.Milliseconds);
            }
            else
            {
                double percentComplete = (currentTimeSpan.TotalSeconds - startingTimeSpan.TotalSeconds) / (endingTimeSpan.TotalSeconds - startingTimeSpan.TotalSeconds);

                int totalIntervals = Math.Abs(pairs[0].Value - pairs[1].Value);
                double secondsBetweenIntervals = Math.Abs((endingTimeSpan - startingTimeSpan).TotalSeconds) / totalIntervals;
                secondsBetweenIntervals = Convert.ToInt32(Math.Ceiling(secondsBetweenIntervals * percentComplete));

                if (secondsBetweenIntervals == 0)
                {
                    secondsBetweenIntervals = 1;
                }

                sleepDuration = TimeSpan.FromSeconds(secondsBetweenIntervals);
            }

            ColorTemperature currentColorTemperature = GetColorTemperature(now);

            // Account for updating the test hook between retrieving color temperature values
            //if (_today.HasValue)
            //{
            //    if (Today.Day != (now + sleepDuration).Day)
            //    {
            //        // Make sure we only update once
            //        Today = Today.AddDays(1);
            //    }
            //}

            ColorTemperature nextColorTemperature = GetColorTemperature(now + sleepDuration);
            if (currentColorTemperature == nextColorTemperature)
            {
                return sleepDuration + GetThreadSleepDuration(now + sleepDuration);
            }

            return sleepDuration;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public ColorTemperature GetColorTemperature(DateTime now)
        {
            List<KeyValuePair<DateTime, int>> pairs = GetSortedTimesAndColorTemperaturePairs(now);

            return GetColorTemperature(pairs[0].Value, pairs[1].Value, now.Subtract(pairs[0].Key).TotalSeconds, pairs[1].Key.Subtract(pairs[0].Key).TotalSeconds);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        public static ColorTemperature GetColorTemperature(int startValue, int endValue, double secondsSinceStart, double totalDurationInSeconds)
        {
            double temperatureRange = Math.Abs(endValue - startValue);
            double percentageComplete = secondsSinceStart / totalDurationInSeconds;

            if (startValue > endValue)
            {
                return Convert.ToInt32(startValue - temperatureRange * percentageComplete);
            }
            else
            {
                return Convert.ToInt32(startValue + temperatureRange * percentageComplete);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        private List<KeyValuePair<DateTime, int>> GetSortedTimesAndColorTemperaturePairs(DateTime now)
        {
            List<KeyValuePair<DateTime, int>> values = null;

            values = InsertInToTimeAndColorTemperaturePair(null, GetSunrise(now), ConvertKelvinToColorTemperature(SunriseColorTemperature));
            values = InsertInToTimeAndColorTemperaturePair(values, GetSolarNoon(now), ConvertKelvinToColorTemperature(SolarNoonTemperature));
            values = InsertInToTimeAndColorTemperaturePair(values, now.Date + StopTime, ConvertKelvinToColorTemperature(StopColorTemperature));

            // No sunset when it occurs after the stop time
            if (GetSunset(now) < now.Date + StopTime)
            {
                values = InsertInToTimeAndColorTemperaturePair(values, GetSunset(now), ConvertKelvinToColorTemperature(SunsetColorTemperature));
            }

            // Duration at night after the stop time should have no adjustments
            values = InsertInToTimeAndColorTemperaturePair(values, GetSunrise(now) - TimeSpan.FromMinutes(30), ConvertKelvinToColorTemperature(StopColorTemperature));

            return GetCurrentDateTimeAndTemperaturePairs(now, values);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="time"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static List<KeyValuePair<DateTime, int>> InsertInToTimeAndColorTemperaturePair(List<KeyValuePair<DateTime, int>> values, DateTime time, int value)
        {
            if (values == null)
            {
                values = new List<KeyValuePair<DateTime, int>>
                {
                    new KeyValuePair<DateTime, int>(time, value)
                };

                return values;
            }

            for (int i = 0; i < values.Count(); i++)
            {
                if (time < values[i].Key)
                {
                    values.Insert(i, new KeyValuePair<DateTime, int>(time, value));
                    return values;
                }
            }

            values.Add(new KeyValuePair<DateTime, int>(time, value));
            return values;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        private static List<KeyValuePair<DateTime, int>> GetCurrentDateTimeAndTemperaturePairs(DateTime now, List<KeyValuePair<DateTime, int>> values)
        {
            List<KeyValuePair<DateTime, int>> pairs = new List<KeyValuePair<DateTime, int>>();

            for (int i = 0; i < values.Count(); i++)
            {
                if (now < values[i].Key)
                {
                    if (i == 0)
                    {
                        KeyValuePair<DateTime, int> previousDay = new KeyValuePair<DateTime, int>(
                            values[values.Count() - 1].Key.Subtract(new TimeSpan(24, 0, 0)),
                            values[values.Count() - 1].Value);

                        pairs.Add(previousDay);
                        pairs.Add(values[i]);
                    }
                    else
                    {
                        pairs.Add(values[i - 1]);
                        pairs.Add(values[i]);
                    }

                    return pairs;
                }
            }

            KeyValuePair<DateTime, int> nextDay = new KeyValuePair<DateTime, int>(
                values[0].Key.Add(new TimeSpan(24, 0, 0)),
                values[0].Value);

            pairs.Add(values[values.Count() - 1]);
            pairs.Add(nextDay);

            return pairs;
        }
    }
}
