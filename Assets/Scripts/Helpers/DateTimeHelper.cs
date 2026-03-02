using System.Globalization;
using System;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// DATETIMEHELPER - Date/time parsing and formatting utilities.
    /// 
    /// PURPOSE:
    /// Provides utilities for parsing timestamps and formatting
    /// elapsed time into human-readable strings.
    /// 
    /// KEY METHODS:
    /// - ParseUtcTimestamp: Converts string to DateTime
    /// - ParseTimeElapsed: Formats elapsed time as "X ago" string
    /// 
    /// USAGE:
    /// ```csharp
    /// DateTime saved = DateTimeHelper.ParseUtcTimestamp(saveFile.Timestamp);
    /// string display = DateTimeHelper.ParseTimeElapsed(saved);
    /// // Result: "2 hours, 15 minutes ago"
    /// ```
    /// 
    /// OUTPUT FORMATS:
    /// - "X seconds ago"
    /// - "X minutes, Y seconds ago"
    /// - "X hours, Y minutes ago"
    /// - "X days, Y hours ago"
    /// - "X months, Y days ago"
    /// 
    /// RELATED FILES:
    /// - SaveFileSelectManager.cs: Displays save timestamps
    /// - DateFormat.cs: Timestamp format constants
    /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>Parses a UTC timestamp string into DateTime.</summary>
        public static DateTime ParseUtcTimestamp(string timestamp)
        {
            return DateTime.ParseExact(
                timestamp,
                DateFormat.yyyyMMddHHmmss,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
            );
        }

        /// <summary>Formats elapsed time since timestamp as human-readable string.</summary>
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
                // Approximate a month as 30 days
                int months = (int)(elapsed.TotalDays / 30);
                int days = (int)(elapsed.TotalDays % 30);
                string monthPart = months == 1 ? "1 month" : $"{months} months";
                string dayPart = days == 1 ? "1 day" : $"{days} days";
                return days > 0 ? $"{monthPart}, {dayPart} ago" : $"{monthPart} ago";
            }
        }


    }


}
