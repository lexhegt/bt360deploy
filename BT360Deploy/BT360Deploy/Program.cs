// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Xml;
using AxonOlympus.BT360Deploy.BizTalkGroupServiceReference;
using Newtonsoft.Json;

namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// This console application enables you to create BizTalk360 Alerts
    /// It supports the Deployment Framework (BTDF)
    /// </summary>
    partial class Program
    {
        static private NetworkCredential networkCredential = null;
        static private ReceivePortCollection receivePorts;
        static private OrchestrationCollection orchestrations;
        static private SendPortCollection sendPorts;
        static private XmlDocument xmlDocument = new XmlDocument();
        static private Options options = new Options();
        static private UserAlarm userAlarm = null;

        static private string baseUrl = "";
        static private string environmentId = "";
        static private bool alarmExists = false;

        /// <summary>
        /// Main method, becomes called on start up.
        /// 
        /// It takes care of the following topics:
        ///  - read/parse commandline arguments
        ///  - load BTDF settings file
        ///  - create BizTalk360 Alert
        ///  - add alert mappings for
        ///    - Receive Ports
        ///    - Send Ports
        ///    - Orchestrations
        ///    - Instance states
        /// </summary>
        /// <param name="args">The command line arguments</param>
        static void Main(string[] args)
        {

            Console.WriteLine("{0}BT360Deploy v{1}.{2}.{3} - Creation of BizTalk360 Alerts{0}(c) 2016 Axon Olympus, Netherlands{0}", Environment.NewLine, Assembly.GetEntryAssembly().GetName().Version.Major, Assembly.GetEntryAssembly().GetName().Version.Minor, Assembly.GetEntryAssembly().GetName().Version.Build);

            // Get command line parameters
            Console.WriteLine("Get parameters");
            CommandLine.Parser.Default.ParseArguments(args, options);
        
            // Exit in case no parameters were passed
            if (string.IsNullOrEmpty(options.BizTalkApplication) || (string.IsNullOrEmpty(options.SettingsFile)))
            {
                Console.WriteLine("Pass parameters:{0} -a <Name of BizTalk Application>{0} -s <Name and location of BTDF Settings file>{0} -e Action on existing alarm: [overwrite]/[update]/[donothing]", Environment.NewLine);
                Environment.ExitCode = ERROR_BAD_ARGUMENTS;
                return;
            }

            Console.WriteLine("- Name BizTalk Application : {0}", options.BizTalkApplication);
            Console.WriteLine("- Environment Settings File: {0}", options.SettingsFile);
            Console.WriteLine("- Action on Existing Alarm : {0}", options.Existing.ToUpper());

            try
            {

                // Load settings file
                LoadSettingsFile(options.SettingsFile);

                Console.WriteLine("- BizTalk360 URL : {0}", baseUrl);
                Console.WriteLine("- BizTalk360 User: {0}{1}", networkCredential.UserName, Environment.NewLine);

                // Check BizTalk360 url
                CheckBizTalk360URL();

                // Check credentials
                CheckUserProfile();

                // Get information about the BizTalk Application
                RetrieveBizTalkApplication();

                // Delete existing Alert
                DeleteUserAlert();

                // If the undeploy parameter has been provide, we're finished
                if (options.Undeploy) { return; }

                // Create (new) User Alert
                CreateUserAlert();

                // If the BizTalk360 Alert already exists and the donothing parameter is provided, we're finished
                if (alarmExists && options.Existing.ToUpper() == DO_NOTHING) { return; }

                // Add Alert Mappings
                ManageAlertMonitorConfig();

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}Exception: {1}", Environment.NewLine, ex.Message);
                return;
            }
        
            Console.WriteLine("{0}Finished {1} alert for BizTalk application '{2}'", Environment.NewLine, alarmExists ? "updating" : "creating", options.BizTalkApplication);
        }
        /// <summary>
        /// Checks the availability of the BizTalk360 URL
        /// </summary>
        static private void CheckBizTalk360URL()
        {
            Console.Write("Check BizTalk360 URL: ");

            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials = networkCredential;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("AdminService.svc", "GetBizTalk360Info"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_ENVIRONMENT_INVALID_OR_NOT_FOUND);
                    Environment.ExitCode = ERROR_ENVIRONMENT_INVALID_OR_NOT_FOUND;
                    throw new Exception("EnvironmentID invalid or not found.");
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                try
                {
                    // Perform the request
                    HttpResponseMessage response = httpClient.GetAsync(urlParameters).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the response object
                        var getBizTalk360InfoResponse = response.Content.ReadAsAsync<GetBizTalk360InfoResponse>().Result;

                        // Check for success and no errors
                        if (getBizTalk360InfoResponse.success && getBizTalk360InfoResponse.errors == null)
                        {
                            // Retrieve BizTalk360Info
                            BizTalk360Info bizTalk360Info = getBizTalk360InfoResponse.bizTalk360Info;

                            Console.WriteLine(SUCCESS);
                            Console.WriteLine("- BizTalk360 Version: {0}", bizTalk360Info.biztalk360Version);
                            Console.WriteLine("- BizTalk360 Edition: {0}", bizTalk360Info.biztalk360Edition);
                            Console.WriteLine("- Application Server: {0}", bizTalk360Info.deployedAppServer);
                            Console.WriteLine("- Database server   : {0}", bizTalk360Info.deployedDBServer);
                            return;
                        }
                        else
                        {
                            throw new Exception(String.Format("{0} (Error: {1})", getBizTalk360InfoResponse.errors[0].errorCode, getBizTalk360InfoResponse.errors[0].description));
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("{0}", response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_BAD_BIZTALK360_URL);
                    throw ex;
                }
            }
        }
        /// <summary>
        /// Checks if the provided User Profile is a valid BizTalk360 User Profile
        /// </summary>
        static private void CheckUserProfile()
        {
            Console.Write("{0}Check BizTalk360 User Profile: ", Environment.NewLine);

            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials = networkCredential;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("AdminService.svc", "GetUserProfile"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_ENVIRONMENT_INVALID_OR_NOT_FOUND);
                    Environment.ExitCode = ERROR_ENVIRONMENT_INVALID_OR_NOT_FOUND;
                    throw new Exception("EnvironmentID invalid or not found.");
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                // List data response.
                try
                {
                    // Perform the request
                    HttpResponseMessage response = httpClient.GetAsync(urlParameters).Result;
                
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the response object
                        var getUserProfileResponse = response.Content.ReadAsAsync<GetUserProfileResponse>().Result;

                        // Check for success and no errors
                        if (getUserProfileResponse.success && getUserProfileResponse.errors == null)
                        {
                            // Retrieve UserProfile
                            UserProfile userProfile = getUserProfileResponse.userProfile;

                            Console.WriteLine(SUCCESS);
                            Console.WriteLine("- Domain\\User Name: {0}\\{1}", userProfile.domainName, userProfile.userName);
                            Console.WriteLine("- Time Zone       : {0} (UTC Offset: {1})", userProfile.timeZoneId, userProfile.utcOffset);
                            Console.WriteLine("- Date Time format: {0}", userProfile.dateTimeFormat);
                            return;
                        }
                        else
                        {
                            Environment.ExitCode = ERROR_WEB_SERVICE_RESPONSE;
                            throw new Exception(String.Format("{0} (Error: {1})", getUserProfileResponse.errors[0].errorCode, getUserProfileResponse.errors[0].description));
                        }
                    }
                    else
                    {
                        Environment.ExitCode = ERROR_WEB_SERVICE_RESPONSE;
                        throw new Exception(String.Format("{0}", response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}, ({1})", FAILED, ERROR_WEB_SERVICE_RESPONSE);
                    Environment.ExitCode = ERROR_WEB_SERVICE_RESPONSE;
                    throw ex;
                }
            }
        }
        /// <summary>
        /// Constructs a string which contains alert mappings for the state of Service Instances
        /// </summary>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string ConstructSerializedConfig()
        {
            string ServiceInstanceTemplate = "\"currentMonitoringStatus\":3,\"warningLevel\":{0},\"errorLevel\":{1},\"serviceInstanceState\":{2},\"serviceInstanceDescription\":\"{3}\",\"currentCount\":0,\"isMonitoringRequired\":{4}";
            bool enableMonitoring = false;
            string serializedConfigServiceLines = "";
            string serializedConfigServiceLine = "";
            int WarningLevel = 0;
            int ErrorLevel = 0;

            for (int i = 0; i <= 5; i++)
            {

                string serviceInstanceDescription = "";
                switch (i)
                {
                    case 0:
                        {
                            serviceInstanceDescription = "Suspended (Resumable)";
                            enableMonitoring = GetProperty("BizTalk360_monitorSuspendedResumable", false);
                            WarningLevel = GetProperty("BizTalk360_suspendedResumableWarningLevel", 10);
                            ErrorLevel = GetProperty("BizTalk360_suspendedResumableErrorLevel", 20);
                            break;
                        }
                    case 1:
                        {
                            serviceInstanceDescription = "Suspended (Not-Resumable)";
                            enableMonitoring = GetProperty("BizTalk360_monitorSuspendedNotResumable", false);
                            WarningLevel = GetProperty("BizTalk360_suspendedNotResumableWarningLevel", 10);
                            ErrorLevel = GetProperty("BizTalk360_suspendedNotResumableErrorLevel", 20);
                            break;
                        }
                    case 2:
                        {
                            serviceInstanceDescription = "Active";
                            enableMonitoring = GetProperty("BizTalk360_monitorActiveInstances", false);
                            WarningLevel = GetProperty("BizTalk360_activeInstancesWarningLevel", 10);
                            ErrorLevel = GetProperty("BizTalk360_activeInstancesErrorLevel", 20);
                            break;
                        }
                    case 3:
                        {
                            serviceInstanceDescription = "Dehydrated";
                            enableMonitoring = GetProperty("BizTalk360_monitorDehydratedInstances", false);
                            WarningLevel = GetProperty("BizTalk360_dehydratedInstancesWarningLevel", 10);
                            ErrorLevel = GetProperty("BizTalk360_dehydratedInstancesErrorLevel", 20);
                            break;
                        }
                    case 4:
                        {
                            serviceInstanceDescription = "Ready To Run";
                            enableMonitoring = GetProperty("BizTalk360_monitorReadyToRunInstances", false);
                            WarningLevel = GetProperty("BizTalk360_readyToRunInstancesWarningLevel", 10);
                            ErrorLevel = GetProperty("BizTalk360_readyToRunInstancesErrorLevel", 20);
                            break;
                        }
                    case 5:
                        {
                            serviceInstanceDescription = "Scheduled";
                            enableMonitoring = GetProperty("BizTalk360_monitorScheduledInstances", false);
                            WarningLevel = GetProperty("BizTalk360_scheduledInstancesWarningLevel", 10);
                            ErrorLevel = GetProperty("BizTalk360_scheduledInstancesErrorLevel", 20);
                            break;
                        }
                }
                serializedConfigServiceLine = String.Format(ServiceInstanceTemplate, WarningLevel, ErrorLevel, i, serviceInstanceDescription, enableMonitoring.ToString().ToLower());
                serializedConfigServiceLines += "{" + serializedConfigServiceLine + "},";
            }

            return "[" + serializedConfigServiceLines + "]";
        }
        /// <summary>
        /// Constructs a string which contains alert mappings for the state of Orchestrations
        /// </summary>
        /// <param name="orchestrations">Collection of Orchestrations of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Orchestrations must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string ConstructSerializedConfig(OrchestrationCollection orchestrations, string expectedState)
        {
            string OrchestrationTemplate = "\"expectedState\":{0},\"setToExpectedState\":{1},\"maxAutoCorrectRetry\":{2},\"currentAutoCorrectCount\":{3},\"name\":\"{4}\",\"status\":{5},\"applicationName\":\"{6}\",\"assemblyQualifiedName\":\"{7}\",\"description\":\"{8}\",\"hostName\":\"{9}\"";

            string serializedConfigServiceLines = "";
            string serializedConfigServiceLine = "";

            int nrOfOchestrations = orchestrations.Count;

            for (int i = 0; i < nrOfOchestrations; i++)
            {
                BizTalkOrchestration bizTalkOrchestration = orchestrations[i];

                int expectedStateOrchestrations = 4;
                switch (GetProperty("BizTalk360_exp_orch_" + bizTalkOrchestration.name, expectedState))
                {
                    case ORCHESTRATION_BOUND: { expectedStateOrchestrations = 1; break; }
                    case ORCHESTRATION_STARTED: { expectedStateOrchestrations = 2; break; }
                    case ORCHESTRATION_STOPPED: { expectedStateOrchestrations = 3; break; }
                    case ORCHESTRATION_UNENLISTED: { expectedStateOrchestrations = 0; break; }
                    case ORCHESTRATION_DO_NOT_MONITOR: { expectedStateOrchestrations = 4; break; }
                    default: { expectedStateOrchestrations = 4; break; }
                }

                string status = "";
                switch (bizTalkOrchestration.status)
                {
                    case OrchestrationStatus.Bound:
                        {
                            status = "0";
                            break;
                        }
                    case OrchestrationStatus.Started:
                        {
                            status = "1";
                            break;
                        }
                    case OrchestrationStatus.Stopped:
                        {
                            status = "2";
                            break;
                        }
                    case OrchestrationStatus.Unbound:
                        {
                            status = "3";
                            break;
                        }
                }
                string host = (bizTalkOrchestration.host == null) ? "" : bizTalkOrchestration.host.name.ToString();
                serializedConfigServiceLine = String.Format(OrchestrationTemplate, expectedStateOrchestrations, "false", "0", "0", bizTalkOrchestration.name, status, bizTalkOrchestration.applicationName, bizTalkOrchestration.assemblyQualifiedName, bizTalkOrchestration.description, host);
                serializedConfigServiceLines += "{" + serializedConfigServiceLine + "},";
            }

            return "[" + serializedConfigServiceLines + "]";
        }
        /// <summary>
        /// Constructs a string which contains alert mappings for the state of Receive Ports
        /// </summary>
        /// <param name="receiveports">Collection of Receive Ports of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Receive Ports must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string ConstructSerializedConfig(ReceivePortCollection receiveports, string expectedState)
        {
            string ReceiveLocationTemplate = "\"expectedState\":{0},\"setToExpectedState\":{1},\"maxAutoCorrectRetry\":{2},\"currentAutoCorrectCount\":{3},\"processMonitorConfigCollection\":null,\"name\":\"{4}\",\"receivePortName\":\"{5}\",\"address\":\"{6}\",\"isPrimary\":{7},\"isEnabled\":{8},\"isTwoWay\":{9}";
            string serializedConfigServiceLines = "";
            string serializedConfigServiceLine = "";

            int nrOfReceivePorts = receiveports.Count;

            for (int i = 0; i < nrOfReceivePorts; i++)
            {
                ReceivePort receiveport = receiveports[i];

                int nrOfReceiveLocations = receiveport.receiveLocations.Count;

                for (int j = 0; j < nrOfReceiveLocations; j++)
                {
                    ReceiveLocation receivelocation = receiveports[i].receiveLocations[j];

                    int expectedStateReceiveLocations = 2;
                    switch (GetProperty("BizTalk360_exp_rl_" + receivelocation.name , expectedState))
                    {
                        case RECEIVE_LOCATION_ENABLED: { expectedStateReceiveLocations = 0; break; }
                        case RECEIVE_LOCATION_DISABLED: { expectedStateReceiveLocations = 1; break; }
                        case RECEIVE_LOCATION_DO_NOT_MONITOR: { expectedStateReceiveLocations = 2; break; }
                        default: { expectedStateReceiveLocations = 2; break; }
                    }

                    serializedConfigServiceLine = string.Format(ReceiveLocationTemplate, expectedStateReceiveLocations, "false", "0", "0", receivelocation.name, receiveport.name, "", receivelocation.isPrimary.ToString().ToLower(), receivelocation.isEnabled.ToString().ToLower(), receivelocation.isTwoWay.ToString().ToLower());
                    serializedConfigServiceLines += "{" + serializedConfigServiceLine + "},";
                }
            }
            return "[" + serializedConfigServiceLines + "]";
        }
        /// <summary>
        /// Constructs a string which contains alert mappings for the state of Send Ports
        /// </summary>
        /// <param name="sendports">Collection of Send Ports of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Send Ports must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string ConstructSerializedConfig(SendPortCollection sendports, string expectedState)
        {
            string SendPortTemplate = "\"expectedState\":{0},\"setToExpectedState\":{1},\"maxAutoCorrectRetry\":{2},\"currentAutoCorrectCount\":{3},\"processMonitorConfigCollection\":null,\"name\":\"{4}\",\"status\":{5},\"isDynamic\":false,\"isTwoWay\":false,\"applicationName\":\"{6}\"";
            string serializedConfigServiceLines = "";
            string serializedConfigServiceLine = "";

            int nrOfSendPorts = sendports.Count;

            for (int i = 0; i < nrOfSendPorts; i++)
            {
                SendPort sendport = sendports[i];

                int expectedStateSendPorts = 4;
                switch (GetProperty("BizTalk360_exp_sp_" + sendport.name, expectedState))
                {
                    case SEND_PORT_STARTED: { expectedStateSendPorts = 2; break; }
                    case SEND_PORT_STOPPED: { expectedStateSendPorts = 3; break; }
                    case SEND_PORT_BOUND: { expectedStateSendPorts = 0; break; }
                    case SEND_PORT_DO_NOT_MONITOR: { expectedStateSendPorts = 4; break; }
                    default: { expectedStateSendPorts = 4; break; }
                }

                string status = "";
                switch (sendport.status)
                {
                    case PortStatus.Bound:
                        {
                            status = "0";
                            break;
                        }
                    case PortStatus.Started:
                        {
                            status = "1";
                            break;
                        }
                    case PortStatus.Stopped:
                        {
                            status = "2";
                            break;
                        }
                }
                serializedConfigServiceLine = String.Format(SendPortTemplate, expectedStateSendPorts, "false", "0", "0", sendport.name, status, sendport.applicationName);
                serializedConfigServiceLines += "{" + serializedConfigServiceLine + "},";
            }

            return "[" + serializedConfigServiceLines + "]";
        }
        /// <summary>
        /// Constructs the URL to be used for making calls to the BizTalk360 API
        /// </summary>
        /// <param name="service">Name of the servce (.svc) to be called</param>
        /// <param name="operation">Name of the operation of the service to be called</param>
        /// <returns>string containing the URL</returns>
        static private string ConstructServiceURL(string service, string operation)
        {
            /* URL consists of the following parts
             * - baseUrl                           : protocol (http(s)), domain name and virtual directory from BizTalk360: comes from setting BizTalk360URL
             * - virtual directory of the services : Services.REST/
             * - name of the service               : ie BizTalkGroupService.svc (see the API documentation for all other services)
             * - name of the operation called      : is GetBizTalkApplicationsList (see the API documentation for all other operations on the services)
             * - parameters                        : at least ?environmentId=<value>, but more parameters might be required, this depends on the operation at hand, see the API documentation
             *                                       the value of the environmentId comes from the settings file
             */

            string url = baseUrl.EndsWith("/") ? string.Format("{0}Services.REST/{1}/{2}", baseUrl, service, operation) : string.Format("{0}/Services.REST/{1}/{2}", baseUrl, service, operation);

            return url;
        }
        /// <summary>
        /// Creates a JSON string which contains a request message and all metadata to create a BizTalk360 alert 
        /// </summary>
        /// <returns>A JSON string with the entire request message to create a BizTalk360 Alert</returns>
        static private string CreateCreateUserAlertRequest()
        {
            // Create User Alarm objects
            UserAlertRequest userAlertRequest = new UserAlertRequest
            {
                name = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication),
                commaSeparatedEmails = GetProperty(BIZTALK360_COMMA_SEPARATED_EMAILS, ""),
                description = GetProperty(BIZTALK360_DESCRIPTION, String.Format("Alert for BizTalk Application '{0}'", options.BizTalkApplication)),
                isAlertDisabled = GetProperty(BIZTALK360_IS_ALERT_DISABLED, true),
                isThresholdRestricted = GetProperty(BIZTALK360_IS_THRESHOLD_RESTRICTED, false), // Threshold Alert - Set alerts on set day(s) and time(s) only  
                alertASAPWaitDurationInMinutes = GetProperty(BIZTALK360_ALERT_ASAP_WAIT_DURATION_IN_MINUTES, 10),
                isContinuousErrorRestricted = GetProperty(BIZTALK360_IS_CONTINUOUS_ERROR_RESTRICTED, false),
                continuousErrorMaxCount = GetProperty(BIZTALK360_CONTINUOUS_ERROR_MAX_COUNT, 3),
                isAlertOnCorrection = GetProperty(BIZTALK360_IS_ALERT_ON_CORRECTION, false),
                isAlertASAP = GetProperty(BIZTALK360_IS_ALERT_ASAP, false),
                thresholdDaysOfWeek = GetProperty(BIZTALK360_THRESHOLD_DAYS_OF_WEEK, new DaysOfWeek { Fri = false, Mon = false, Sat = false, Sun = false, Thu = false, Tue = false, Wed = false }),
                thresholdRestrictStartTime = GetProperty(BIZTALK360_THRESHOLD_RESTRICTED_START_TIME, new TimeSpan(0, 0, 0)),
                thresholdRestrictEndTime = GetProperty(BIZTALK360_THRESHOLD_RESTRICTED_END_TIME, new TimeSpan(0, 0, 0)),
                isAlertHealthMonitoring = GetProperty(BIZTALK360_IS_ALERT_HEALTH_MONITORING, false),
                daysValidation = GetProperty(BIZTALK360_DAYS_VALIDATION, "day"),
                daysOfWeek = GetProperty(BIZTALK360_DAYS_OF_WEEK, new DaysOfWeek { Fri = false, Mon = false, Sat = false, Sun = false, Thu = false, Tue = false, Wed = false }),
                timeOfDays = GetProperty(BIZTALK360_TIME_OF_DAYS, new TimeOfDays { Eight = false, Eighteen = false, Eleven = false, Fifteen = false, Five = false, Four = false, Fourteen = false, Nine = false, Nineteen = false, One = false, Seven = false, Seventeen = false, Six = false, Sixteen = false, Ten = false, Thirteen = false, Three = false, Twelve = false, Twenty = false, TwentyOne = false, TwentyThree = false, TwentyTwo = false, Two = false, Zero = false }),
                isAlertProcessMonitoring = GetProperty(BIZTALK360_IS_ALERT_PROCESS_MONITORING, false),
                isAlertProcessMonitoringOnSuccess = GetProperty(BIZTALK360_IS_ALERT_PROCESS_MONITORING_ON_SUCCESS, false),
                commaSeparatedSMSNumbers = GetProperty(BIZTALK360_COMMA_SEPARATED_SMS_NUMBERS, ""),
                isAlertHPOMEnabled = GetProperty(BIZTALK360_IS_ALERT_HPOM_ENABLED, false),
                isAlertEventVwrEnabled = GetProperty(BIZTALK360_IS_ALERT_EVENT_VWR_ENABLED, false),
                eventId = GetProperty(BIZTALK360_EVENT_ID, ""),
                isTestMode = GetProperty(BIZTALK360_IS_TEST_MODE, false),
                notificationChannels = new List<string>(),
                createdBy = networkCredential.UserName
            };

            // Create the Request
            string request = CreateUserAlertRequest(userAlertRequest);

            return request;
        }
        /// <summary>
        /// Create a JSON string which contains a message to delete a BizTalk360 Alert
        /// </summary>
        /// <param name="userAlertName">Name of the BizTalk360 Alert which has to be deleted</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/DeleteUserAlarm</returns>
        static private string CreateDeleteUserAlertRequest(string userAlertName)
        {
            DeleteUserAlertRequest deleteUserAlertRequest = new DeleteUserAlertRequest
            {
                context = new Context
                {
                    callerReference = "DELETE-USER-ALERT-REQUEST",
                    environmentSettings = new EnvironmentSettings
                    {
                        id = Guid.Parse(environmentId),
                        licenseEdition = 0
                    }
                },
                alarmName = userAlertName
            };
            return JsonConvert.SerializeObject(deleteUserAlertRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Orchestrations
        /// </summary>
        /// <param name="orchestrations">Collection of Orchestrations of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Orchestrations must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateOrchestrationMappings(OrchestrationCollection orchestrations, string expectedState)
        {
            string serializedMonitorConfig = ConstructSerializedConfig(orchestrations, expectedState);

            AlertMappingRequest alertMappingRequest = new AlertMappingRequest
            {
                context = new Context
                {
                    callerReference = "ORCHESTRATION-MAPPING",
                    environmentSettings = new EnvironmentSettings
                    {
                        id = Guid.Parse(environmentId),
                        licenseEdition = 0
                    }
                },
                monitorGroupName = options.BizTalkApplication,
                monitorGroupType = "Application",
                monitorName = "Orchestrations",
                operation = 0,
                alarmName = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication),
                serializedMonitorConfigforApplicationOrchestration = serializedMonitorConfig,
                serializedJsonMonitorConfig = serializedMonitorConfig
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Receive Ports
        /// </summary>
        /// <param name="receivePorts">Collection of Receive Ports of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Receive Ports must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateReceivePortsMappings(ReceivePortCollection receivePorts, string expectedState)
        {

            string serializedMonitorConfig = ConstructSerializedConfig(receivePorts, expectedState);

            AlertMappingRequest alertMappingRequest = new AlertMappingRequest
            {
                context = new Context
                {
                    callerReference = "RECEIVELOCATION-MAPPING",
                    environmentSettings = new EnvironmentSettings
                    {
                        id = Guid.Parse(environmentId),
                        licenseEdition = 0
                    }
                },
                monitorGroupName = options.BizTalkApplication,
                monitorGroupType = "Application",
                monitorName = "ReceiveLocations",
                operation = 0,
                alarmName = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication),
                serializedMonitorConfigforApplicationReceiveLocation = serializedMonitorConfig,
                serializedJsonMonitorConfig = serializedMonitorConfig
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Send Ports
        /// </summary>
        /// <param name="sendPorts">Collection of Send Ports of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Send Ports must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateSendPortMappings(SendPortCollection sendports, string expectedState)
        {
            string serializedMonitorConfig = ConstructSerializedConfig(sendports, expectedState);

            AlertMappingRequest alertMappingRequest = new AlertMappingRequest
            {
                context = new Context
                {
                    callerReference = "SENDPORTS-MAPPING",
                    environmentSettings = new EnvironmentSettings
                    {
                        id = Guid.Parse(environmentId),
                        licenseEdition = 0
                    }
                },
                monitorGroupName = options.BizTalkApplication,
                monitorGroupType = "Application",
                monitorName = "SendPorts",
                operation = 0,
                alarmName = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication),
                serializedMonitorConfigforApplicationSendPorts = serializedMonitorConfig,
                serializedJsonMonitorConfig = serializedMonitorConfig
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Service Instances
        /// </summary>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateServiceInstanceMappings()
        {
            string serializedMonitorConfig = ConstructSerializedConfig();

            AlertMappingRequest alertMappingRequest = new AlertMappingRequest
            {
                context = new Context
                {
                    callerReference = "SERVICEINSTANCE-MAPPING",
                    environmentSettings = new EnvironmentSettings
                    {
                        id = Guid.Parse(environmentId),
                        licenseEdition = 0
                    }
                },
                monitorGroupName = options.BizTalkApplication,
                monitorGroupType = "Application",
                monitorName = "Service Instances",
                operation = 0,
                alarmName = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication),
                serializedMonitorConfigforApplicationServiceInstance = serializedMonitorConfig,
                serializedJsonMonitorConfig = serializedMonitorConfig
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Creates a JSON string which contains a request message and all metadata to update an existing BizTalk360 alert 
        /// </summary>
        /// <returns>A JSON string with the entire request message to update an existing BizTalk360 Alert</returns>
        static private string CreateUpdateUserAlertRequest()
        {
            
            // Create User Alarm objects
            UserAlertRequest userAlertRequest = new UserAlertRequest
            {
                name = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication),
                commaSeparatedEmails = GetProperty(BIZTALK360_COMMA_SEPARATED_EMAILS, userAlarm.commaSeparatedEmails),
                description = GetProperty(BIZTALK360_DESCRIPTION, String.Format("Alert for BizTalk Application '{0}'", options.BizTalkApplication)),
                isAlertDisabled = GetProperty(BIZTALK360_IS_ALERT_DISABLED, userAlarm.isAlertDisabled),
                isThresholdRestricted = GetProperty(BIZTALK360_IS_THRESHOLD_RESTRICTED, userAlarm.isThresholdRestricted), // Threshold Alert - Set alerts on set day(s) and time(s) only  
                alertASAPWaitDurationInMinutes = GetProperty(BIZTALK360_ALERT_ASAP_WAIT_DURATION_IN_MINUTES, userAlarm.alertASAPWaitDurationInMinutes),
                isContinuousErrorRestricted = GetProperty(BIZTALK360_IS_CONTINUOUS_ERROR_RESTRICTED, userAlarm.isContinuousErrorRestricted),
                continuousErrorMaxCount = GetProperty(BIZTALK360_CONTINUOUS_ERROR_MAX_COUNT, userAlarm.continuousErrorMaxCount),
                isAlertOnCorrection = GetProperty(BIZTALK360_IS_ALERT_ON_CORRECTION, userAlarm.isAlertOnCorrection),
                isAlertASAP = GetProperty(BIZTALK360_IS_ALERT_ASAP, userAlarm.isAlertASAP),
                thresholdDaysOfWeek = GetProperty(BIZTALK360_THRESHOLD_DAYS_OF_WEEK, userAlarm.thresholdDaysOfWeek != null ? new DaysOfWeek(userAlarm.thresholdDaysOfWeek): new DaysOfWeek()),
                thresholdRestrictStartTime = GetProperty(BIZTALK360_THRESHOLD_RESTRICTED_START_TIME, new TimeSpan(8, 0, 0)),
                thresholdRestrictEndTime = GetProperty(BIZTALK360_THRESHOLD_RESTRICTED_END_TIME, new TimeSpan(18, 0, 0)),
                isAlertHealthMonitoring = GetProperty(BIZTALK360_IS_ALERT_HEALTH_MONITORING, userAlarm.isAlertHealthMonitoring),
                daysValidation = GetProperty(BIZTALK360_DAYS_VALIDATION, "day"),
                daysOfWeek = GetProperty(BIZTALK360_DAYS_OF_WEEK, userAlarm.daysOfWeek != null ? new DaysOfWeek(userAlarm.daysOfWeek) : new DaysOfWeek()),
                timeOfDays = GetProperty(BIZTALK360_TIME_OF_DAYS, new TimeOfDays { Eight = userAlarm.timeOfDays.Eight, Eighteen = userAlarm.timeOfDays.Eighteen, Eleven = userAlarm.timeOfDays.Eleven, Fifteen = userAlarm.timeOfDays.Fifteen, Five = userAlarm.timeOfDays.Five, Four = userAlarm.timeOfDays.Four, Fourteen = userAlarm.timeOfDays.Fourteen, Nine = userAlarm.timeOfDays.Nine, Nineteen = userAlarm.timeOfDays.Nineteen, One = userAlarm.timeOfDays.One, Seven = userAlarm.timeOfDays.Seven, Seventeen = userAlarm.timeOfDays.Seventeen, Six = userAlarm.timeOfDays.Six, Sixteen = userAlarm.timeOfDays.Sixteen, Ten = userAlarm.timeOfDays.Ten, Thirteen = userAlarm.timeOfDays.Thirteen, Three = userAlarm.timeOfDays.Three, Twelve = userAlarm.timeOfDays.Twelve, Twenty = userAlarm.timeOfDays.Twenty, TwentyOne = userAlarm.timeOfDays.TwentyOne, TwentyThree = userAlarm.timeOfDays.TwentyThree, TwentyTwo = userAlarm.timeOfDays.TwentyTwo, Two = userAlarm.timeOfDays.Two, Zero = userAlarm.timeOfDays.Zero }),
                isAlertProcessMonitoring = GetProperty(BIZTALK360_IS_ALERT_PROCESS_MONITORING, userAlarm.isAlertProcessMonitoring),
                isAlertProcessMonitoringOnSuccess = GetProperty(BIZTALK360_IS_ALERT_PROCESS_MONITORING_ON_SUCCESS, userAlarm.isAlertProcessMonitoringOnSuccess),
                commaSeparatedSMSNumbers = GetProperty(BIZTALK360_COMMA_SEPARATED_SMS_NUMBERS, userAlarm.commaSeparatedSMSNumbers),
                isAlertHPOMEnabled = GetProperty(BIZTALK360_IS_ALERT_HPOM_ENABLED, userAlarm.isAlertHPOMEnabled),
                isAlertEventVwrEnabled = GetProperty(BIZTALK360_IS_ALERT_EVENT_VWR_ENABLED, userAlarm.isAlertEventVwrEnabled),
                eventId = GetProperty(BIZTALK360_EVENT_ID, userAlarm.eventId),
                isTestMode = GetProperty(BIZTALK360_IS_TEST_MODE, userAlarm.isTestMode),
                notificationChannels = new List<string>(),
                createdBy = networkCredential.UserName
            };

            // Create the Request
            string request = CreateUserAlertRequest(userAlertRequest);

            return request;
        }
        /// <summary>
        /// Creates a BizTalk360 Alert
        /// Parameters are retrieved from the Settings file of the Deployment Framework
        /// If an Alert with the same name already exists, it will be deleted
        /// </summary>
        /// <returns>True if created, false if not created</returns>
        static private Boolean CreateUserAlert()
        {
            Boolean success = false;

            try
            {
                // If the alarm already exists, then update the alarm or do nothing
                if (alarmExists && options.Existing.ToUpper() != OVERWRITE_ALERT)
                {
                    // Update the alert based on the settings from the settings file or the current alarm
                    if (options.Existing.ToUpper() == UPDATE_ALERT)
                    {
                        // Update User Alert
                        Console.Write("{0}Updating alert '{1}': ", Environment.NewLine, GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication));

                        string userAlertRequest = CreateUpdateUserAlertRequest();
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/UpdateUserAlarm", baseUrl), "POST", userAlertRequest);

                        Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);
                    }
                    else
                    {
                        // Do nothing
                        Console.WriteLine("{0}BizTalk360 Alert '{1}' already exists.{0}Overwriting or updating the alert was not requested.{0}{0}Finished processing.", Environment.NewLine, userAlarm.name);
                    }
                }
                else
                {
                    // Create User Alert
                    Console.Write("{0}Creating alert '{1}': ", Environment.NewLine, GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication));

                    string userAlertRequest = CreateCreateUserAlertRequest();
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/CreateUserAlarm", baseUrl), "POST", userAlertRequest);

                    Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_CREATE_ALERT);
                Environment.ExitCode = ERROR_CREATE_ALERT;
                throw ex;
            }

            return (success);
        }
        /// <summary>
        /// Creates a JSON string which contains a request message to create a BizTalk360 Alert
        /// </summary>
        /// <param name="userAlertRequest">The BizTalk360 Alert for which a request message must be created</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/CreateUserAlarm</returns>
        static private string CreateUserAlertRequest(UserAlertRequest userAlertRequest)
        {
            Request request = new Request
            {
                context = new Context
                {
                    environmentSettings = new EnvironmentSettings
                    {
                        id = Guid.Parse(environmentId),
                        licenseEdition = 0
                    }
                },
                alarm = userAlertRequest
            };
            return JsonConvert.SerializeObject(request);
        }
        /// <summary>
        /// Deletes a BizTalk360 Alert
        /// </summary>
        /// <returns>True if succeeded, false if not succeeded</returns>
        static private bool DeleteUserAlert()
        {
            try
            {
                // Get current BizTalk360 Alerts
                UserAlarms userAlarms = GetUserAlarms();
                string alertName = GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication);

                bool success = false;

                // If it exists, delete the current alarm
                if (userAlarms.Exists(x => x.name == alertName))
                {
                    // Used later on to determine if the alert has to be created or updated
                    alarmExists = true;
                    int i = userAlarms.FindIndex(x => x.name == alertName);
                    userAlarm = userAlarms[i];

                    // Only delete the alert when the appropriate values has been provided
                    if (options.Existing.ToUpper() == OVERWRITE_ALERT)
                    {
                        // Delete existing alert
                        Console.Write("{0}Deleting existing alert '{1}': ", Environment.NewLine, alertName);

                        string deleteUserAlertRequest = CreateDeleteUserAlertRequest(alertName);
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/DeleteUserAlarm", baseUrl), "POST", deleteUserAlertRequest);

                        Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);
                        return (success);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_DELETE_ALERT);
                Environment.ExitCode = ERROR_DELETE_ALERT;
                throw ex;
            }
            return (true);
        }
        /// <summary>
        /// Retrieve the credentials from the setting file and store them in a NetworkCredential
        /// If no credentials are supplied, the credentials of the current user are used
        /// </summary>
        static void GetCredentials()
        {
            string tmpBizTalk360User = GetProperty("BizTalk360_user", "");
            string tmpBizTalk360UserPassword = GetProperty("BizTalk360_userPassword", "");

            // If no credentials were supplied in the Settings file, use the credentials of the current user
            if (string.IsNullOrEmpty(tmpBizTalk360User) || string.IsNullOrEmpty(tmpBizTalk360UserPassword))
            {
                networkCredential = CredentialCache.DefaultNetworkCredentials;
            }
            else
            {
                networkCredential = new NetworkCredential(tmpBizTalk360User, tmpBizTalk360UserPassword);
            }
        }
        /// <summary>
        /// Retrieves a collection containing the Orchestrations of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Orchestrations have to be retrieved</param>
        /// <returns>The collection of Orchestrations for the given BizTalk Application</returns>
        static private OrchestrationCollection GetOrchestrations()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials= networkCredential;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("BizTalkApplicationService.svc", "GetOrchestrations"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    return (null);
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                try
                {
                    // Perform the request
                    HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}&applicationName={1}", urlParameters, options.BizTalkApplication)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the response object
                        var getOrchestrationsResponse = response.Content.ReadAsAsync<GetOrchestrationsResponse>().Result;

                        if (!getOrchestrationsResponse.success || getOrchestrationsResponse.errors != null)
                        {
                            throw new Exception(String.Format("{0} (Error: {1})", getOrchestrationsResponse.errors[0].errorCode, getOrchestrationsResponse.errors[0].description));
                        }

                        // Return the result
                        return getOrchestrationsResponse.orchestrations;
                    }
                    else
                    {
                        throw new Exception(String.Format("{0} (Error: {1})", (int)response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Environment.ExitCode = ERROR_GET_ORCHESTRATIONS;
                    throw ex;
                }
                return null;
            }
        }
        /// <summary>
        /// Reads a property from the Deployment Framework settings file
        /// </summary>
        /// <param name="property">Name of the property which has to be retrieved</param>
        /// <param name="def">Default value, in case the given property could not be retrieved</param>
        /// <returns>The value from the Deployment Framework settings file or the default value</returns>
        static private bool GetProperty(string property, bool def)
        {
            try
            {

                int value = Convert.ToInt16(xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText);

                bool ret = false;
                switch (value)
                {
                    case 0:
                        {
                            ret = false;
                            break;
                        }
                    case 1:
                        {
                            ret = true;
                            break;
                        }
                }
                return (ret);
            }
            catch (Exception)
            { }
            return (def);
        }
        /// <summary>
        /// Reads a property from the Deployment Framework settings file
        /// </summary>
        /// <param name="property">Name of the property which has to be retrieved</param>
        /// <param name="def">Default value, in case the given property could not be retrieved</param>
        /// <returns>The value from the Deployment Framework settings file or the default value</returns>
        static private DaysOfWeek GetProperty(string property, DaysOfWeek def)
        {
            try
            {
                string value = xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText;
                DaysOfWeek daysOfWeek = new DaysOfWeek(value);

                return (daysOfWeek);
            }
            catch (Exception)
            { }
            return (def);
        }
        /// <summary>
        /// Reads a property from the Deployment Framework settings file
        /// </summary>
        /// <param name="property">Name of the property which has to be retrieved</param>
        /// <param name="def">Default value, in case the given property could not be retrieved</param>
        /// <returns>The value from the Deployment Framework settings file or the default value</returns>
        static private int GetProperty(string property, int def)
        {
            try
            {
                int value = Convert.ToInt16(xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText);
                return (value);
            }
            catch (Exception)
            { }
            return (def);
        }
        /// <summary>
        /// Reads a property from the Deployment Framework settings file
        /// </summary>
        /// <param name="property">Name of the property which has to be retrieved</param>
        /// <param name="def">Default value, in case the given property could not be retrieved</param>
        /// <returns>The value from the Deployment Framework settings file or the default value</returns>
        static private string GetProperty(string property, string def)
        {
            try
            {
                string value = xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText;
                return (value);
            }
            catch (Exception)
            { }
            return (def);
        }
        /// <summary>
        /// Reads a property from the Deployment Framework settings file
        /// </summary>
        /// <param name="property">Name of the property which has to be retrieved</param>
        /// <param name="def">Default value, in case the given property could not be retrieved</param>
        /// <returns>The value from the Deployment Framework settings file or the default value</returns>
        static private TimeOfDays GetProperty(string property, TimeOfDays def)
        {
            try
            {
                string value = xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText;
                TimeOfDays timeOfDays = new TimeOfDays(value);

                return (timeOfDays);
            }
            catch (Exception)
            { }
            return (def);
        }
        /// <summary>
        /// Reads a property from the Deployment Framework settings file
        /// </summary>
        /// <param name="property">Name of the property which has to be retrieved</param>
        /// <param name="def">Default value, in case the given property could not be retrieved</param>
        /// <returns>The value from the Deployment Framework settings file or the default value</returns>
        static private string GetProperty(string property, TimeSpan def)
        {
            try
            {
                string value = xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText;

                DateTime dateTime = Convert.ToDateTime(value);
                return ToUnixTimeStamp(dateTime);
            }
            catch (Exception)
            { }

            return ToUnixTimeStamp(def);
        }
        /// <summary>
        /// Convert DateTime object to Unix timestamp
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static private string ToUnixTimeStamp(DateTime dateTime)
        {

            Int32 unixTimestamp = (Int32)(DateTime.Parse("1/12/1968 " + dateTime.Hour + ":" + dateTime.Minute).ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            return "/Date(" + unixTimestamp + "000+0000)/";
        }
        /// <summary>
        /// Convert TimeSpan object to Unix timestamp
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        static private string ToUnixTimeStamp(TimeSpan timeSpan)
        {

            Int32 unixTimestamp = (Int32)(DateTime.Parse("1/12/1968 " + timeSpan.Hours + ":" + timeSpan.Minutes).ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            return "/Date(" + unixTimestamp + "000+0000)/";
        }
        /// <summary>
        /// Retrieves a collection containing the Receive Ports of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Receive Ports have to be retrieved</param>
        /// <returns>The collection of Receive Ports for the given BizTalk Application</returns>
        static private ReceivePortCollection GetReceivePorts()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials = networkCredential;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("BizTalkApplicationService.svc", "GetReceivePorts"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    return (null);
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                try
                {
                    // Perform the request
                    HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}&applicationName={1}", urlParameters, options.BizTalkApplication)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the response object
                        var getReceivePortsResponse = response.Content.ReadAsAsync<GetReceivePortsResponse>().Result;

                        if (!getReceivePortsResponse.success || getReceivePortsResponse.errors != null)
                        {
                            throw new Exception(String.Format("{0} (Error: {1})", getReceivePortsResponse.errors[0].errorCode, getReceivePortsResponse.errors[0].description));
                        }

                        // Return the result.
                        return getReceivePortsResponse.receivePorts;
                    }
                    else
                    {
                        throw new Exception(String.Format("{0} (Error: {1})", (int)response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Environment.ExitCode = ERROR_GET_RECEIVE_LOCATIONS;
                    throw ex;
                }
                return null;
            }
        }
        /// <summary>
        /// Retrieves a collection containing the Send Ports of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Send Ports have to be retrieved</param>
        /// <returns>The collection of Send Ports for the given BizTalk Application</returns>
        static private SendPortCollection GetSendPorts()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials= networkCredential;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("BizTalkApplicationService.svc", "GetSendPorts"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    return (null);
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                try
                {
                    // Perform the request
                    HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}&applicationName={1}", urlParameters, options.BizTalkApplication)).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        // Get the response object
                        var getSendPortsResponse = response.Content.ReadAsAsync<GetSendPortsResponse>().Result;

                        if (!getSendPortsResponse.success || getSendPortsResponse.errors != null)
                        {
                            throw new Exception(String.Format("{0} (Error: {1})", getSendPortsResponse.errors[0].errorCode, getSendPortsResponse.errors[0].description));
                        }

                        // Return the result.
                        return getSendPortsResponse.sendPorts;
                    }
                    else
                    {
                        throw new Exception(String.Format("{0} (Error: {1})", (int)response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Environment.ExitCode = ERROR_GET_SEND_PORTS;
                    throw ex;
                }
                return null;
            }
        }
        static private string GetSetting(string setting)
        {
            XmlNode xmlNode = xmlDocument.SelectSingleNode(String.Format("/settings/property[@name='{0}']", setting));

            if (xmlNode == null)
            {
                throw new Exception(String.Format("Setting '{0}' not found.", setting));
            }

            return (xmlNode.InnerText);

        }
        /// <summary>
        /// Retrieves all BizTalk360 Alerts from the given BizTalk360 environment
        /// </summary>
        /// <returns>An object containing the BizTalk360 Alerts</returns>
        static private UserAlarms GetUserAlarms()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Credentials= networkCredential;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("AlertService.svc", "GetUserAlarms"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    return (null);
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(urlParameters).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Get the response object
                    var getUserAlarmsResponse = response.Content.ReadAsAsync<GetUserAlarmsResponse>().Result;
                    if (getUserAlarmsResponse.success)
                    {
                        // Return the result.
                        return getUserAlarmsResponse.userAlarms;
                    }
                }
                else
                {
                    Console.WriteLine("{0} (Error: {1})", (int)response.StatusCode, response.ReasonPhrase);
                }
            }

            return (null);
        }
        /// <summary>
        /// Loads the Deployment Framework settings file
        /// </summary>
        /// <param name="settingsFile">True if succeeded, false if not succeeded</param>
        static private Boolean LoadSettingsFile(string settingsFile)
        {
            try
            {
                Console.Write("{0}Load settings file: ", Environment.NewLine);
                xmlDocument.Load(settingsFile);

                baseUrl = GetSetting("BizTalk360_url");
                environmentId = GetSetting("BizTalk360_environmentId");

                GetCredentials();

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_BAD_SETTINGSFILE);
                Environment.ExitCode = ERROR_BAD_SETTINGSFILE;
                throw ex;
            }

            Console.WriteLine(SUCCESS);
            return true;
        }
        /// <summary>
        /// Creates mappings for different kind of artifacts and adds them to the created BizTalk360 Alert
        /// </summary>
        /// <returns></returns>
        static private Boolean ManageAlertMonitorConfig()
        {
            string expectedStateReceiveLocations = RECEIVE_LOCATION_ENABLED;
            string expectedStateOrchestrations = ORCHESTRATION_STARTED;
            string expectedStateSendPorts = SEND_PORT_STARTED;
            string request = "";
            Boolean success = false;

            Console.WriteLine("{0}Add mappings to alert '{1}'", Environment.NewLine, GetProperty(BIZTALK360_ALERT_NAME, options.BizTalkApplication));

            try
            {
                // If the BizTalk Application contains Receive Ports, create Alert Mappings
                if (receivePorts.Count > 0)
                {

                    // Don't add mappings if the Receive Locations don't have to be monitored
                    if (expectedStateReceiveLocations != RECEIVE_LOCATION_DO_NOT_MONITOR)
                        { 
                        // Create Receive Ports mappings to the Alert
                        Console.Write("- Adding {0} Receive Port(s) to alert: ", receivePorts.Count);

                        request = CreateReceivePortsMappings(receivePorts, GetProperty("BizTalk360_expectedStateReceiveLocations", RECEIVE_LOCATION_DO_NOT_MONITOR));
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);
                        
                        Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);

                        if (!success) { return false; }
                    }
                    else
                    {
                        Console.WriteLine("- Receive Location monitoring not configured");
                    }
                }

                // If the BizTalk Application contains Orchestrations, create Alert Mappings
                if (orchestrations.Count > 0)
                {

                    // Don't add mappings if the Orchestrations don't have to be monitored
                    if (expectedStateOrchestrations != ORCHESTRATION_DO_NOT_MONITOR)
                    {
                        // Create Orchestration mappings to the Alert
                        Console.Write("- Adding {0} Orchestration(s) to alert: ", orchestrations.Count);

                        request = CreateOrchestrationMappings(orchestrations, GetProperty("BizTalk360_expectedStateOrchestrations", ORCHESTRATION_DO_NOT_MONITOR));
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                        Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);

                        if (!success) { return false; }
                    }
                    else
                    {
                        Console.WriteLine("- Orchestration monitoring not configured");
                    }
                }

                // If the BizTalk Application contains Send Ports, create Alert Mappings
                if (sendPorts.Count > 0)
                {

                    // Don't add mappings if the Send Ports don't have to be monitored
                    if (expectedStateSendPorts != SEND_PORT_DO_NOT_MONITOR)
                    {
                        // Create Send Port mappings to the Alert
                        Console.Write("- Adding {0} Send Port(s) to alert: ", sendPorts.Count);

                        request = CreateSendPortMappings(sendPorts, GetProperty("BizTalk360_expectedStateSendPorts", SEND_PORT_DO_NOT_MONITOR));
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                        Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);

                        if (!success) { return false; }
                    }
                    else
                    {
                        Console.WriteLine("- Send Port monitoring not configured");
                    }
                }

                // If the BizTalk Application contains Orchestrations or Send Ports, create Alert Mappings for Suspended Instances
                if ((orchestrations.Count > 0 && expectedStateOrchestrations != ORCHESTRATION_DO_NOT_MONITOR) || (sendPorts.Count > 0 && expectedStateSendPorts != SEND_PORT_DO_NOT_MONITOR))
                {
                    Console.Write("- Adding Instance Warning/Error Levels to alert: ");

                    // Create Instance mappings to the Alert
                    request = CreateServiceInstanceMappings();
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                    Console.WriteLine("{0}", success == true ? SUCCESS : FAILED);

                    if (!success) { return false; }
                }
                else
                {
                    Console.WriteLine("- Instance State monitoring not required");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} (Error: {1})", FAILED, ERROR_MAPPING_ALERT);
                Environment.ExitCode = ERROR_MAPPING_ALERT;
                throw ex;
            }

            return true;
        }
        /// <summary>
        /// Processes requests and responses to the BizTalk360 API
        /// </summary>
        /// <param name="url">Base URL of the BizTalk360 API</param>
        /// <param name="method">The method which will be called</param>
        /// <param name="data">The request message which will be provided to the method</param>
        /// <returns></returns>
        static public Boolean ProcessResponse(string url, string method, string data)
        {
            // Create WebRequest Object
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Credentials = networkCredential;
            webRequest.Method = method;
            webRequest.ContentType = "application/json";
            webRequest.ContentLength = data.Length;

            using (Stream stream = webRequest.GetRequestStream())
            using (StreamWriter streamWriter = new StreamWriter(stream, System.Text.Encoding.ASCII))
                streamWriter.Write(data);

            // Response Object
            WebResponse webResponse= null;
            Response response = null;

            try
            {
                // Perform the request
                webResponse = webRequest.GetResponse();
            }
            catch (Exception ex) 
            {
                throw ex;
            }

            // Process the answer of the request
            string responseFromServer = "";
            using (Stream stream = webResponse.GetResponseStream())
            {
                if (stream != null)
                {
                    using (StreamReader responseReader = new StreamReader(stream))
                    {
                        responseFromServer = responseReader.ReadToEnd();

                        // Deserialize JSON to a Response object
                        response = JsonConvert.DeserializeObject<Response>(responseFromServer);
                        responseReader.Close();
                    }
                }
            }
            webResponse.Close();

            if (!response.success || response.errors != null)
            {
                throw new Exception(response.errors[0].description);
            }

            return (response.success);
        }
        /// <summary>
        /// Retrieves the Receive/Send Ports and Orchestrations of the BizTalk Application at hand.
        /// Information about the retrieval of BizTalk artifacts is shown on screen
        /// </summary>
        /// <returns>True if Receive/Send Ports or Orchestrations were found, false if not found</returns>
        static public Boolean RetrieveBizTalkApplication()
        {

            Console.WriteLine("{0}Retrieve information about the BizTalk Application", Environment.NewLine);

            try
            {
                // Get all Receive Ports
                Console.WriteLine("- Receive Ports");
                receivePorts = GetReceivePorts();

                // Get all Orchestrations
                Console.WriteLine("- Orchestrations");
                orchestrations = GetOrchestrations();

                // Get all Send Ports
                Console.WriteLine("- Send Ports");
                sendPorts = GetSendPorts();

                // If the BizTalk Application contains no Receive Ports, Orchestrations or Send Ports, exit
                if (receivePorts.Count == 0 && orchestrations.Count == 0 && sendPorts.Count == 0)
                {
                    throw new Exception((String.Format("No Receive/Send Ports and Orchestrations found => no alert created{0}{0}", Environment.NewLine)));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }
    }
}
