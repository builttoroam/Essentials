using System;

namespace Xamarin.Essentials
{
    public static partial class CalendarExtensions
    {
        public static TimeSpan RoundToNearestMinutes(this TimeSpan input, int minutes)
        {
            var totalMinutes = (int)(input + new TimeSpan(0, minutes / 2, 0)).TotalMinutes;

            return new TimeSpan(0, totalMinutes - (totalMinutes % minutes), 0);
        }

        public static string ToOrdinal(this int num)
        {
            var modedNum = num % 10;
            if (((num / 10) % 10) == 1)
            {
                return $"{num}th";
            }
            switch (modedNum)
            {
                case 1: return $"{num}st";
                case 2: return $"{num}nd";
                case 3: return $"{num}rd";
                default: return $"{num}th";
            }
        }
    }
}
