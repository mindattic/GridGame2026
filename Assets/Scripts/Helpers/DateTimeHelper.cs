using System.Globalization;
using System;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
{
    public static class DateTimeHelper
    {

        public static DateTime ParseUtcTimestamp(string timestamp)
        {
            return DateTime.ParseExact(
                timestamp,
                DateFormat.yyyyMMddHHmmss,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
            );
        }

        public static string ParseTimeElapsed(DateTime timestamp)
        {
            TimeSpan elapsed = DateTime.UtcNow - timestamp;

            if (elapsed.TotalSeconds < 60)
            {
                int seconds = (int)elapsed.TotalSeconds;
                return seconds == 1 ? "1 second ago" : $"{seconds} seconds ago";
            }
            else if (elapsed.TotalMinutes < 60)
            {
                int minutes = (int)elapsed.TotalMinutes;
                int seconds = elapsed.Seconds;
                string minutePart = minutes == 1 ? "1 minute" : $"{minutes} minutes";
                string secondPart = seconds == 1 ? "1 second" : $"{seconds} seconds";
                return seconds > 0 ? $"{minutePart}, {secondPart} ago" : $"{minutePart} ago";
            }
            else if (elapsed.TotalHours < 24)
            {
                int hours = (int)elapsed.TotalHours;
                int minutes = elapsed.Minutes;
                string hourPart = hours == 1 ? "1 hour" : $"{hours} hours";
                string minutePart = minutes == 1 ? "1 minute" : $"{minutes} minutes";
                return minutes > 0 ? $"{hourPart}, {minutePart} ago" : $"{hourPart} ago";
            }
            else if (elapsed.TotalDays < 30)
            {
                int days = (int)elapsed.TotalDays;
                int hours = elapsed.Hours;
                string dayPart = days == 1 ? "1 day" : $"{days} days";
                string hourPart = hours == 1 ? "1 hour" : $"{hours} hours";
                return hours > 0 ? $"{dayPart}, {hourPart} ago" : $"{dayPart} ago";
            }
            else
            {
                // Approximate a month as 30 days.
                int months = (int)(elapsed.TotalDays / 30);
                int days = (int)(elapsed.TotalDays % 30);
                string monthPart = months == 1 ? "1 month" : $"{months} months";
                string dayPart = days == 1 ? "1 day" : $"{days} days";
                return days > 0 ? $"{monthPart}, {dayPart} ago" : $"{monthPart} ago";
            }
        }


    }


}
