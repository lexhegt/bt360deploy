using System;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains all hours of the day
    /// This must be provided while creating a BizTalk360 Alert
    /// </summary>
    public class TimeOfDays
    {
        public TimeOfDays() { }
        public TimeOfDays(string values)
        {
            // Eight = true Eighteen = true, Eleven = false, Fifteen = false, Five = false, Four = false, Fourteen = false, Nine = false, Nineteen = false, One = false, Seven = false, Seventeen = false, Six = false, Sixteen = false, Ten = false, Thirteen = false, Three = false, Twelve = false, Twenty = false, TwentyOne = false, TwentyThree = false, TwentyTwo = false, Two = false, Zero = false
            string[] timeOfDays = values.Split(',');

            foreach (string timeOfDay in timeOfDays)
            {
                try
                {
                    if (timeOfDay.ToLower().Trim().Substring(0, 4) == "zero") { Zero = Convert.ToBoolean(timeOfDay.Substring(7)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 3) == "one") { One = Convert.ToBoolean(timeOfDay.Substring(6)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 3) == "two") { Two = Convert.ToBoolean(timeOfDay.Substring(6)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 5) == "three") { Three = Convert.ToBoolean(timeOfDay.Substring(8)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 4) == "five") { Five = Convert.ToBoolean(timeOfDay.Substring(7)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 3) == "ten") { Ten = Convert.ToBoolean(timeOfDay.Substring(6)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 6) == "eleven") { Eleven = Convert.ToBoolean(timeOfDay.Substring(9)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 6) == "twelve") { Twelve = Convert.ToBoolean(timeOfDay.Substring(9)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 8) == "thirteen") { Thirteen = Convert.ToBoolean(timeOfDay.Substring(11)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 8) == "fourteen") { Fourteen = Convert.ToBoolean(timeOfDay.Substring(11)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 4) == "four") { Four = Convert.ToBoolean(timeOfDay.Substring(7)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 7) == "fifteen") { Fifteen = Convert.ToBoolean(timeOfDay.Substring(10)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 7) == "sixteen") { Sixteen = Convert.ToBoolean(timeOfDay.Substring(10)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 3) == "six") { Six = Convert.ToBoolean(timeOfDay.Substring(6)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 9) == "seventeen") { Seventeen = Convert.ToBoolean(timeOfDay.Substring(12)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 5) == "seven") { Seven = Convert.ToBoolean(timeOfDay.Substring(8)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 8) == "eighteen") { Eighteen = Convert.ToBoolean(timeOfDay.Substring(11)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 5) == "eight") { Eight = Convert.ToBoolean(timeOfDay.Substring(8)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 8) == "nineteen") { Nineteen = Convert.ToBoolean(timeOfDay.Substring(11)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 4) == "nine") { Nine = Convert.ToBoolean(timeOfDay.Substring(7)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 11) == "twentythree") { TwentyThree = Convert.ToBoolean(timeOfDay.Substring(14)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 9) == "twentytwo") { TwentyTwo = Convert.ToBoolean(timeOfDay.Substring(12)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 9) == "twentyone") { TwentyOne = Convert.ToBoolean(timeOfDay.Substring(12)); continue; };
                    if (timeOfDay.ToLower().Trim().Substring(0, 6) == "twenty") { Twenty = Convert.ToBoolean(timeOfDay.Substring(9)); continue; };
                }
                catch (Exception)
                { continue; }
            }
        }
        public override string ToString()
        {
            return (String.Format("Zero = {0}, One = {1}, Two = {2}, Three = {3}, Four = {4}, Five = {5}, Six = {6}, Seven = {7}, Eight = {8}, Nine = {9}, Ten = {10}, Eleven = {11}, Twelve = {12}, ThirTeen = {13}, FourTeen = {14}, FifTeen = {15}, SixTeen = {16}, SevenTeen = {17}, EighTeen = {18}, NineTeen = {19}, Twenty = {20}, TwentyOne = {21}, TwentyTwo = {22}, TwentyThree = {23}", Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Eleven, Twelve, Thirteen, Fourteen, Fifteen, Sixteen, Seventeen, Eighteen, Nineteen, Twenty, TwentyOne, TwentyTwo, TwentyThree));
        }
        public bool Zero { get; set; }
        public bool One { get; set; }
        public bool Two { get; set; }
        public bool Three { get; set; }
        public bool Four { get; set; }
        public bool Five { get; set; }
        public bool Six { get; set; }
        public bool Seven { get; set; }
        public bool Eight { get; set; }
        public bool Nine { get; set; }
        public bool Ten { get; set; }
        public bool Eleven { get; set; }
        public bool Twelve { get; set; }
        public bool Thirteen { get; set; }
        public bool Fourteen { get; set; }
        public bool Fifteen { get; set; }
        public bool Sixteen { get; set; }
        public bool Seventeen { get; set; }
        public bool Eighteen { get; set; }
        public bool Nineteen { get; set; }
        public bool Twenty { get; set; }
        public bool TwentyOne { get; set; }
        public bool TwentyTwo { get; set; }
        public bool TwentyThree { get; set; }
    }
}
