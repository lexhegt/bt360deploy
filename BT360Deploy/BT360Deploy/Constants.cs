using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxonOlympus.BT360Deploy
{

    public partial class Program
    {
        // Definition of constants
        const string SUCCESS = "SUCCESS";
        const string FAILED = "FAILED";

        const int ERROR_BAD_ARGUMENTS = 1;
        const int ERROR_BAD_SETTINGSFILE = 2;
        const int ERROR_BAD_BIZTALK360_URL = 3;
        const int ERROR_BAD_USER = 4;
        const int ERROR_DELETE_ALERT = 5;
        const int ERROR_CREATE_ALERT = 6;
        const int ERROR_ENVIRONMENT_INVALID_OR_NOT_FOUND = 7;
        const int ERROR_WEB_SERVICE_RESPONSE = 8;
        const int ERROR_GET_RECEIVE_LOCATIONS = 9;
        const int ERROR_GET_ORCHESTRATIONS = 10;
        const int ERROR_GET_SEND_PORTS = 11;
        const int ERROR_MAPPING_ALERT = 12;

        const string RECEIVE_LOCATION_ENABLED = "Enabled";
        const string RECEIVE_LOCATION_DISABLED = "Disabled";
        const string RECEIVE_LOCATION_DO_NOT_MONITOR = "Do not monitor";

        const string ORCHESTRATION_BOUND = "Bound";
        const string ORCHESTRATION_STARTED = "Started";
        const string ORCHESTRATION_STOPPED = "Stopped";
        const string ORCHESTRATION_UNENLISTED = "Unenlisted";
        const string ORCHESTRATION_DO_NOT_MONITOR = "Do not monitor";

        const string SEND_PORT_BOUND = "Bound";
        const string SEND_PORT_STARTED = "Started";
        const string SEND_PORT_STOPPED = "Stopped";
        const string SEND_PORT_DO_NOT_MONITOR = "Do not monitor";

        const string BIZTALK360_ALERT_NAME = "BizTalk360_alertName";
        const string BIZTALK360_COMMA_SEPARATED_EMAILS = "BizTalk360_commaSeparatedEmails";
        const string BIZTALK360_DESCRIPTION = "BizTalk360_description";
        const string BIZTALK360_IS_ALERT_DISABLED = "BizTalk360_isAlertDisabled";
        const string BIZTALK360_IS_THRESHOLD_RESTRICTED = "BizTalk360_isThresholdRestricted";
        const string BIZTALK360_ALERT_ASAP_WAIT_DURATION_IN_MINUTES = "BizTalk360_alertASAPWaitDurationInMinutes";
        const string BIZTALK360_IS_CONTINUOUS_ERROR_RESTRICTED = "BizTalk360_isContinuousErrorRestricted";
        const string BIZTALK360_CONTINUOUS_ERROR_MAX_COUNT = "BizTalk360_continuousErrorMaxCount";
        const string BIZTALK360_IS_ALERT_ON_CORRECTION = "BizTalk360_isAlertOnCorrection";
        const string BIZTALK360_IS_ALERT_ASAP = "BizTalk360_isAlertASAP";
        const string BIZTALK360_THRESHOLD_DAYS_OF_WEEK = "BizTalk360_thresholdDaysOfWeek";
        const string BIZTALK360_THRESHOLD_RESTRICTED_START_TIME = "BizTalk360_thresholdRestrictStartTime";
        const string BIZTALK360_THRESHOLD_RESTRICTED_END_TIME = "BizTalk360_thresholdRestrictEndTime";
        const string BIZTALK360_IS_ALERT_HEALTH_MONITORING = "BizTalk360_isAlertHealthMonitoring";
        const string BIZTALK360_DAYS_VALIDATION = "BizTalk360_daysValidation";
        const string BIZTALK360_DAYS_OF_WEEK = "BizTalk360_daysOfWeek";
        const string BIZTALK360_TIME_OF_DAYS = "BizTalk360_timeOfDays";
        const string BIZTALK360_IS_ALERT_PROCESS_MONITORING = "BizTalk360_isAlertProcessMonitoring";
        const string BIZTALK360_IS_ALERT_PROCESS_MONITORING_ON_SUCCESS = "BizTalk360_isAlertProcessMonitoringOnSuccess";
        const string BIZTALK360_COMMA_SEPARATED_SMS_NUMBERS = "BizTalk360_commaSeparatedSMSNumbers";
        const string BIZTALK360_IS_ALERT_HPOM_ENABLED = "BizTalk360_isAlertHPOMEnabled";
        const string BIZTALK360_IS_ALERT_EVENT_VWR_ENABLED = "BizTalk360_isAlertEventVwrEnabled";
        const string BIZTALK360_EVENT_ID = "BizTalk360_eventId";
        const string BIZTALK360_IS_TEST_MODE = "BizTalk360_isTestMode";
    }
}
