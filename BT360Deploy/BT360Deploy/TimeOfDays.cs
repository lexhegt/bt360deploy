// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
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
            string[] timeOfDays = values.Split(',');

            // Eight = true, Eighteen = true, Eleven = false, Fifteen = false, Five = false, Four = false, Fourteen = false, Nine = false, Nineteen = false, One = false, Seven = false, Seventeen = false, Six = false, Sixteen = false, Ten = false, Thirteen = false, Three = false, Twelve = false, Twenty = false, TwentyOne = false, TwentyThree = false, TwentyTwo = false, Two = false, Zero = false
            foreach (string timeOfDay in timeOfDays)
            {
                string[] timeOfDaysParts = values.Split('=');
                try
                {
                    if (timeOfDaysParts[0].ToLower().Trim() == "zero") { Zero = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "one") { One = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "three") { Three = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "five") { Five = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "ten") { Ten = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "eleven") { Eleven = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "twelve") { Twelve = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "thirteen") { Thirteen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "fourteen") { Fourteen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "four") { Four = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "fifteen") { Fifteen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "sixteen") { Sixteen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "six") { Six = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "seventeen") { Seventeen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "seven") { Seven = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "eighteen") { Eighteen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "eight") { Eight = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "nineteen") { Nineteen = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "nine") { Nine = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "twentythree") { TwentyThree = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "twentytwo") { TwentyTwo = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "twentyone") { TwentyOne = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
                    if (timeOfDaysParts[0].ToLower().Trim() == "twenty") { Twenty = Convert.ToBoolean(timeOfDaysParts[1].Trim()); continue; };
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
