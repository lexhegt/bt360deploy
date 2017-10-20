// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
using System;
using System.Collections.Generic;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the fields of BizTalk360 Alerts
    /// </summary>
    public class UserAlertRequest
    {
        public string id { get; set; }
        public string alarmId { get; set; }
        public string name { get; set; }
        public string commaSeparatedEmails { get; set; }
        public string commaSeparatedSMSNumbers { get; set; }
        public bool isTestMode { get; set; }
        public bool isAlertASAP { get; set; }
        public int alertASAPWaitDurationInMinutes { get; set; }
        public int alertASAPErrorDetectionCount { get; set; }
        public bool isContinuousErrorRestricted { get; set; }
        public int continuousErrorMaxCount { get; set; }
        public bool isAlertOnCorrection { get; set; }
        public DaysOfWeek daysOfWeek { get; set; }
        public TimeOfDays timeOfDays { get; set; }
        public bool isAlertProcessMonitoring { get; set; }
        public bool isAlertProcessMonitoringOnSuccess { get; set; }
        public bool isAlertHealthMonitoring { get; set; }
        public bool isAlertHPOMEnabled { get; set; }
        public bool isAlertRestEndpointEnabled { get; set; }
        public bool isAlertEventVwrEnabled { get; set; }
        public string eventId { get; set; }
        public bool isAlertDisabled { get; set; }
        public string createdBy { get; set; }
        public string description { get; set; }
        public bool isThresholdRestricted { get; set; }
        public string thresholdRestrictStartTime { get; set; }
        public string thresholdRestrictEndTime { get; set; }
        public string daysValidation { get; set; }
        public DaysOfWeek thresholdDaysOfWeek { get; set; }
        public List<String> notificationChannels { get; set; }
    }
}
