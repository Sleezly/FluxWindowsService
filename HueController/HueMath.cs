using System;
using System.Collections.Generic;
using System.Linq;

namespace Hue
{
    public class HueMath
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="time1"></param>
        /// <param name="time2"></param>
        /// <param name="color1"></param>
        /// <param name="color2"></param>
        /// <returns></returns>
        public static TimeSpan CalculateSleepDuration(TimeSpan now, TimeSpan time1, TimeSpan time2, int color1, int color2)
        {

            //TimeSpan sleepDuration = HueMath.CalculateSleepDuration(
            //    currentTimeSpan,
            //    startingTimeSpan,
            //    endingTimeSpan,
            //    pairs[0].Value,
            //    pairs[1].Value);

            int totalIntervals = 0;

            if (time1 == time2)
            {
                throw new ArgumentException($"Thw two provided time values of '{time1.ToString()}' should not match.");
            }
            else if (color1 == color2)
            {
                totalIntervals = 1;
            }
            else
            {
                totalIntervals = Math.Abs(color1 - color2);
            }

            int secondsBetweenIntervals = Convert.ToInt32(Math.Ceiling(Math.Abs((time2 - time1).TotalSeconds) / totalIntervals));

            TimeSpan sleepTime = TimeSpan.FromSeconds(secondsBetweenIntervals);

            if (now + sleepTime > time2)
             {
                // The 1 second must be accounted for by the caller
                return time2 - now + TimeSpan.FromSeconds(1);
             }
            else
            {
                return sleepTime;
            }
        }
    }
}
