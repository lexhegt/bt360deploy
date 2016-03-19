// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
using System;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains all the days of the week
    /// This must be provided while creating a BizTalk360 Alert
    /// </summary>
    public class DaysOfWeek
    {
        public DaysOfWeek() { }
        public DaysOfWeek(AxonOlympus.BT360Deploy.BizTalkGroupServiceReference.DaysOfWeek daysOfWeek)
        {
            Sun = daysOfWeek.Sun;
            Sat = daysOfWeek.Sat;
            Mon = daysOfWeek.Mon;
            Tue = daysOfWeek.Tue;
            Wed = daysOfWeek.Wed;
            Thu = daysOfWeek.Thu;
            Fri = daysOfWeek.Fri;
        }
        public DaysOfWeek(string values)
        {
            // Fri = false, Mon = false, Sat = false, Sun = false, Thu = false, Tue = false, Wed = false
            string[] days = values.Split(',');

            foreach (string day in days)
            {
                switch (day.Trim().Substring(0, 3).ToLower())
                {
                    case "sun": { Sun = Convert.ToBoolean(day.Substring(6)); break; }
                    case "mon": { Mon = Convert.ToBoolean(day.Substring(6)); break; }
                    case "tue": { Tue = Convert.ToBoolean(day.Substring(6)); break; }
                    case "wed": { Wed = Convert.ToBoolean(day.Substring(6)); break; }
                    case "thu": { Thu = Convert.ToBoolean(day.Substring(6)); break; }
                    case "fri": { Fri = Convert.ToBoolean(day.Substring(6)); break; }
                    case "sat": { Sat = Convert.ToBoolean(day.Substring(6)); break; }
                }
            }
        }
        public override string ToString()
        {
            return (string.Format("Fri = {0}, Mon = {1}, Sat = {2}, Sun = {3}, Thu = {4}, Tue = {5}, Wed = {6}", Fri, Mon, Sat, Sun, Thu, Tue, Wed));
        }
        public bool Mon { get; set; }
        public bool Tue { get; set; }
        public bool Wed { get; set; }
        public bool Thu { get; set; }
        public bool Fri { get; set; }
        public bool Sat { get; set; }
        public bool Sun { get; set; }
    }
}
