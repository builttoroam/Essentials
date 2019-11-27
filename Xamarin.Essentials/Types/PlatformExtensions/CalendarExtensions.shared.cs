﻿using System;

namespace Xamarin.Essentials
{
    public static partial class CalendarExtensions
    {
        public static TimeSpan RoundToNearestMinutes(this TimeSpan input, int minutes)
        {
            var totalMinutes = (int)(input + new TimeSpan(0, minutes / 2, 0)).TotalMinutes;

            return new TimeSpan(0, totalMinutes - (totalMinutes % minutes), 0);
        }
    }
}
