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
    class Program
    {
        static private ReceivePortCollection receivePorts;
        static private OrchestrationCollection orchestrations;
        static private SendPortCollection sendPorts;
        static private XmlDocument xmlDocument = new XmlDocument();
        static private Options options = new Options();

        static private string baseUrl = "";
        static private string environmentId = "";
        static private string BizTalk360User = "";
        static private string BizTalk360UserPassword = "";

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

            Console.WriteLine("{0}BT360Deploy v{1}.{2} - Creation of BizTalk360 Alerts{0}", Environment.NewLine, Assembly.GetEntryAssembly().GetName().Version.Major, Assembly.GetEntryAssembly().GetName().Version.Minor);

            // Get command line parameters
            Console.WriteLine("Get parameters");
            CommandLine.Parser.Default.ParseArguments(args, options);
        
            // Exit in case no parameters were passed
            if (string.IsNullOrEmpty(options.BizTalkApplication) || (string.IsNullOrEmpty(options.SettingsFile)))
            {
                Console.WriteLine("Pass parameters:{0} -a <Name of BizTalk Application>{0} -s <Name and location of BTDF Settings file>", Environment.NewLine);
                return;
            }

            Console.WriteLine("- Name BizTalk Application : {0}", options.BizTalkApplication);
            Console.WriteLine("- Environment Settings File: {0}", options.SettingsFile);

            try
            {

                // Load settings file
                LoadSettingsFile(options.SettingsFile);

                Console.WriteLine("- BizTalk360 URL : {0}", baseUrl);
                Console.WriteLine("- BizTalk360 User: {0}{1}", BizTalk360User, Environment.NewLine);

                // Check BizTalk360 url
                CheckBizTalk360URL();

                // Check credentials
                CheckUserProfile();

                // Get information about the BizTalk Application
                RetrieveBizTalkApplication();

                // Delete existing Alert
                DeleteUserAlert();

                // Create (new) User Alert
                CreateUserAlert();

                // Add Alert Mappings
                ManageAlertMonitorConfig();

            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}Exception: {1}", Environment.NewLine, ex.Message);
                return;
            }
            
            Console.WriteLine("{0}Finished creating alert '{1}'", Environment.NewLine, options.BizTalkApplication);
        }
        /// <summary>
        /// Checks the availability of the BizTalk360 URL
        /// </summary>
        static private void CheckBizTalk360URL()
        {
            Console.Write("Check BizTalk360 URL: ");

            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;

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
                    Console.WriteLine(Constants.FAILED);
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

                            Console.WriteLine(Constants.SUCCESS);
                            Console.WriteLine("- BizTalk360 Version: {0}", bizTalk360Info.biztalk360Version);
                            Console.WriteLine("- BizTalk360 Edition: {0}", bizTalk360Info.biztalk360Edition);
                            Console.WriteLine("- Application Server: {0}", bizTalk360Info.deployedAppServer);
                            Console.WriteLine("- Database server   : {0}", bizTalk360Info.deployedDBServer);
                            return;
                        }
                        else
                        {
                            throw new Exception(String.Format("{0} ({1})", getBizTalk360InfoResponse.errors[0].errorCode, getBizTalk360InfoResponse.errors[0].description));
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("{0}", response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Constants.FAILED);
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
            handler.UseDefaultCredentials = true;

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
                    Console.WriteLine(Constants.FAILED);
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

                            Console.WriteLine(Constants.SUCCESS);
                            Console.WriteLine("- Domain\\User Name: {0}\\{1}", userProfile.domainName, userProfile.userName);
                            Console.WriteLine("- Time Zone       : {0} ({1})", userProfile.timeZoneId, userProfile.utcOffset);
                            Console.WriteLine("- Date Time format: {0}", userProfile.dateTimeFormat);
                            return;
                        }
                        else
                        {
                            throw new Exception(String.Format("{0} ({1})", getUserProfileResponse.errors[0].errorCode, getUserProfileResponse.errors[0].description));
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("{0}", response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(Constants.FAILED);
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
        static private string ConstructSerializedConfig(OrchestrationCollection orchestrations, int expectedState)
        {
            string OrchestrationTemplate = "\"expectedState\":{0},\"setToExpectedState\":{1},\"maxAutoCorrectRetry\":{2},\"currentAutoCorrectCount\":{3},\"name\":\"{4}\",\"status\":{5},\"applicationName\":\"{6}\",\"assemblyQualifiedName\":\"{7}\",\"description\":\"{8}\",\"hostName\":\"{9}\"";

            string serializedConfigServiceLines = "";
            string serializedConfigServiceLine = "";

            int nrOfOchestrations = orchestrations.Count;

            for (int i = 0; i < nrOfOchestrations; i++)
            {
                BizTalkOrchestration bizTalkOrchestration = orchestrations[i];

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
                serializedConfigServiceLine = String.Format(OrchestrationTemplate, expectedState.ToString(), "false", "0", "0", bizTalkOrchestration.name, status, bizTalkOrchestration.applicationName, bizTalkOrchestration.assemblyQualifiedName, bizTalkOrchestration.description, host);
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
        static private string ConstructSerializedConfig(ReceivePortCollection receiveports, int expectedState)
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
                    serializedConfigServiceLine = String.Format(ReceiveLocationTemplate, expectedState.ToString(), "false", "0", "0", receivelocation.name, receiveport.name, "", receivelocation.isPrimary.ToString().ToLower(), receivelocation.isEnabled.ToString().ToLower(), receivelocation.isTwoWay.ToString().ToLower());
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
        static private string ConstructSerializedConfig(SendPortCollection sendports, int expectedState)
        {
            string SendPortTemplate = "\"expectedState\":{0},\"setToExpectedState\":{1},\"maxAutoCorrectRetry\":{2},\"currentAutoCorrectCount\":{3},\"processMonitorConfigCollection\":null,\"name\":\"{4}\",\"status\":{5},\"isDynamic\":false,\"isTwoWay\":false,\"applicationName\":\"{6}\"";
            string serializedConfigServiceLines = "";
            string serializedConfigServiceLine = "";

            int nrOfSendPorts = sendports.Count;

            for (int i = 0; i < nrOfSendPorts; i++)
            {
                SendPort sendport = sendports[i];

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
                serializedConfigServiceLine = String.Format(SendPortTemplate, expectedState.ToString(), "false", "0", "0", sendport.name, status, sendport.applicationName);
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
                name = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                commaSeparatedEmails = GetProperty("BizTalk360_commaSeparatedEmails", ""),
                description = GetProperty("BizTalk360_description", String.Format("Alert for BizTalk Application '{0}'", options.BizTalkApplication)),
                isAlertDisabled = GetProperty("BizTalk360_isAlertDisabled", true),
                isThresholdRestricted = GetProperty("BizTalk360_isThresholdRestricted", false), // Threshold Alert - Set alerts on set day(s) and time(s) only  
                alertASAPWaitDurationInMinutes = GetProperty("BizTalk360_alertASAPWaitDurationInMinutes", 10),
                isContinuousErrorRestricted = GetProperty("BizTalk360_isContinuousErrorRestricted", false),
                continuousErrorMaxCount = GetProperty("BizTalk360_continuousErrorMaxCount", 3),
                isAlertOnCorrection = GetProperty("BizTalk360_isAlertOnCorrection", false),
                isAlertASAP = GetProperty("BizTalk360_isAlertASAP", false),
                thresholdDaysOfWeek = GetProperty("BizTalk360_thresholdDaysOfWeek", new DaysOfWeek { Fri = false, Mon = false, Sat = false, Sun = false, Thu = false, Tue = false, Wed = false }),
                thresholdRestrictStartTime = GetProperty("BizTalk360_thresholdRestrictStartTime", new TimeSpan(0, 0, 0)),
                thresholdRestrictEndTime = GetProperty("BizTalk360_thresholdRestrictEndTime", new TimeSpan(0, 0, 0)),
                isAlertHealthMonitoring = GetProperty("BizTalk360_isAlertHealthMonitoring", false),
                daysValidation = GetProperty("BizTalk360_daysValidation", "day"),
                daysOfWeek = GetProperty("BizTalk360_daysOfWeek", new DaysOfWeek { Fri = false, Mon = false, Sat = false, Sun = false, Thu = false, Tue = false, Wed = false }),
                timeOfDays = GetProperty("BizTalk360_timeOfDays", new TimeOfDays { Eight = false, Eighteen = false, Eleven = false, Fifteen = false, Five = false, Four = false, Fourteen = false, Nine = false, Nineteen = false, One = false, Seven = false, Seventeen = false, Six = false, Sixteen = false, Ten = false, Thirteen = false, Three = false, Twelve = false, Twenty = false, TwentyOne = false, TwentyThree = false, TwentyTwo = false, Two = false, Zero = false }),
                isAlertProcessMonitoring = GetProperty("BizTalk360_isAlertProcessMonitoring", false),
                isAlertProcessMonitoringOnSuccess = GetProperty("BizTalk360_isAlertProcessMonitoringOnSuccess", false),
                commaSeparatedSMSNumbers = GetProperty("BizTalk360_commaSeparatedSMSNumbers", ""),
                isAlertHPOMEnabled = GetProperty("BizTalk360_isAlertHPOMEnabled", false),
                isAlertEventVwrEnabled = GetProperty("BizTalk360_isAlertEventVwrEnabled", false),
                eventId = GetProperty("BizTalk360_eventId", ""),
                isTestMode = GetProperty("BizTalk360_isTestMode", false),
                notificationChannels = new List<string>(),
                //isAlertRestEndpointEnabled = false,
                createdBy = BizTalk360User
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
        static private string CreateOrchestrationMappings(OrchestrationCollection orchestrations, int expectedState)
        {

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
                monitorGroupName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                monitorGroupType = "Application",
                monitorName = "Orchestrations",
                operation = 0,
                alarmName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                serializedMonitorConfigforApplicationOrchestration = ConstructSerializedConfig(orchestrations, expectedState),
                serializedJsonMonitorConfig = ConstructSerializedConfig(orchestrations, expectedState)
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Receive Ports
        /// </summary>
        /// <param name="receivePorts">Collection of Receive Ports of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Receive Ports must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateReceivePortsMappings(ReceivePortCollection receivePorts, int expectedState)
        {

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
                monitorGroupName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                monitorGroupType = "Application",
                monitorName = "ReceiveLocations",
                operation = 0,
                alarmName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                serializedMonitorConfigforApplicationReceiveLocation = ConstructSerializedConfig(receivePorts, expectedState),
                serializedJsonMonitorConfig = ConstructSerializedConfig(receivePorts, expectedState)
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Send Ports
        /// </summary>
        /// <param name="sendPorts">Collection of Send Ports of the BizTalk Application which is deployed</param>
        /// <param name="expectedState">State in which the Send Ports must be for this alert</param>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateSendPortMappings(SendPortCollection sendports, int expectedState)
        {

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
                monitorGroupName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                monitorGroupType = "Application",
                monitorName = "SendPorts",
                operation = 0,
                alarmName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                serializedMonitorConfigforApplicationSendPorts = ConstructSerializedConfig(sendports, expectedState),
                serializedJsonMonitorConfig = ConstructSerializedConfig(sendports, expectedState)
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
        }
        /// <summary>
        /// Constructs a JSON string which contains alert mappings for the state of Service Instances
        /// </summary>
        /// <returns>JSON string to be used in the request message of AlertService.svc/ManageAlertMonitorConfig</returns>
        static private string CreateServiceInstanceMappings()
        {
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
                monitorGroupName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                monitorGroupType = "Application",
                monitorName = "Service Instances",
                operation = 0,
                alarmName = GetProperty("BizTalk360_alertName", options.BizTalkApplication),
                serializedMonitorConfigforApplicationServiceInstance = ConstructSerializedConfig(),
                serializedJsonMonitorConfig = ConstructSerializedConfig()
            };

            return JsonConvert.SerializeObject(alertMappingRequest);
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
                // Create User Alert
                Console.Write("{0}Creating alert '{1}': ", Environment.NewLine, options.BizTalkApplication);

                string userAlertRequest = CreateCreateUserAlertRequest();
                success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/CreateUserAlarm", baseUrl), "POST", userAlertRequest);

                Console.WriteLine("{0}", success == true ? Constants.SUCCESS : Constants.FAILED);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Constants.FAILED);
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
        static private Boolean DeleteUserAlert()
        {
            try
            {
                // Get current BizTalk360 Alerts
                UserAlarms userAlarms = GetUserAlarms();
                string alertName = GetProperty("BizTalk360_alertName", options.BizTalkApplication);

                Boolean success = false;

                // If it exists, delete the current alarm
                if (userAlarms.Exists(x => x.name == alertName))
                {
                    // Delete existing alert
                    Console.Write("{0}Deleting existing alert '{1}': ", Environment.NewLine, alertName);

                    string deleteUserAlertRequest = CreateDeleteUserAlertRequest(alertName);
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/DeleteUserAlarm", baseUrl), "POST", deleteUserAlertRequest);

                    Console.WriteLine("{0}", success == true ? Constants.SUCCESS : Constants.FAILED);
                    return (success);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Constants.FAILED);
                throw ex;
            }
            return (true);
        }
        /// <summary>
        /// Retrieves a collection containing the Orchestrations of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Orchestrations have to be retrieved</param>
        /// <returns>The collection of Orchestrations for the given BizTalk Application</returns>
        static private OrchestrationCollection GetOrchestrations()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;

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
                        if (getOrchestrationsResponse.success)
                        {
                            // Return the result.
                            return getOrchestrationsResponse.orchestrations;
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
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
            Int32 unixTimestamp = 0;
            try
            {
                string value = xmlDocument.SelectSingleNode(string.Format("/settings/property[@name='{0}']", property)).InnerText;

                DateTime dateTime = Convert.ToDateTime(value);
                //DateTime dt = new DateTime(1, 1, 1, dateTime.Hour, dateTime.Minute, 0).AddTicks(117780000000);

                unixTimestamp = (Int32)(DateTime.Parse("1/12/1968 " + dateTime.Hour + ":" + dateTime.Minute).ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                return "/Date(" + unixTimestamp + "000+0000)/";
            }
            catch (Exception)
            { }

            unixTimestamp = (Int32)(DateTime.Parse("1/12/1968 0:00").ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
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
            handler.UseDefaultCredentials = true;

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
                        if (getReceivePortsResponse.success)
                        {
                            // Return the result.
                            return getReceivePortsResponse.receivePorts;
                        }
                    }
                    else
                    {
                        throw new Exception(String.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
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
            handler.UseDefaultCredentials = true;

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
                        if (getSendPortsResponse.success)
                        {
                            // Return the result.
                            return getSendPortsResponse.sendPorts;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                        throw new Exception(String.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase));
                    }
                }
                catch (Exception ex)
                {
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
            handler.UseDefaultCredentials = true;

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
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
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

                environmentId = GetSetting("BizTalk360_environmentId");
                baseUrl = GetSetting("BizTalk360_url");
                BizTalk360User = GetSetting("BizTalk360_user");
                BizTalk360UserPassword = GetSetting("BizTalk360_userPassword");
            }
            catch (Exception ex)
            {
                Console.WriteLine(Constants.FAILED);
                throw ex;
            }

            Console.WriteLine(Constants.SUCCESS);
            return true;
        }
        /// <summary>
        /// Creates mappings for different kind of artifacts and adds them to the created BizTalk360 Alert
        /// </summary>
        /// <returns></returns>
        static private Boolean ManageAlertMonitorConfig()
        {
            int expectedStateReceiveLocations = 0;
            int expectedStateOrchestrations = 0;
            int expectedStateSendPorts = 0;
            string request = "";
            Boolean success = false;

            Console.WriteLine("{0}Add mappings to alert '{1}'", Environment.NewLine, options.BizTalkApplication);

            try
            {
                // If the BizTalk Application contains Receive Ports, create Alert Mappings
                if (receivePorts.Count > 0)
                {
                    switch (GetProperty("BizTalk360_expectedStateReceiveLocations", "Do not monitor"))
                    {
                        case "Enabled": { expectedStateReceiveLocations = 0; break; }
                        case "Disabled": { expectedStateReceiveLocations = 1; break; }
                        case "Do not monitor": { expectedStateReceiveLocations = 2; break; }
                        default: { expectedStateReceiveLocations = 2; break; }
                    }

                    // Don't add mappings if the Receive Locations don't have to be monitored
                    if (expectedStateReceiveLocations != 2)
                        { 
                        // Create Receive Ports mappings to the Alert
                        Console.Write("- Adding {0} Receive Port(s) to alert: ", receivePorts.Count);

                        request = CreateReceivePortsMappings(receivePorts, expectedStateReceiveLocations);
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);
                        
                        Console.WriteLine("{0}", success == true ? Constants.SUCCESS : Constants.FAILED);

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
                    switch (GetProperty("BizTalk360_expectedStateOrchestrations", "Do not monitor"))
                    {
                        case "Bound": { expectedStateOrchestrations = 1; break; }
                        case "Started": { expectedStateOrchestrations = 2; break; }
                        case "Stopped": { expectedStateOrchestrations = 3; break; }
                        case "Unenlisted": { expectedStateOrchestrations = 0; break; }
                        case "Do not monitor": { expectedStateOrchestrations = 4; break; }
                        default: { expectedStateOrchestrations = 4; break; }
                    }

                    // Don't add mappings if the Orchestrations don't have to be monitored
                    if (expectedStateOrchestrations != 4)
                    {
                        // Create Orchestration mappings to the Alert
                        Console.Write("- Adding {0} Orchestration(s) to alert: ", orchestrations.Count);

                        request = CreateOrchestrationMappings(orchestrations, expectedStateOrchestrations);
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                        Console.WriteLine("{0}", success == true ? Constants.SUCCESS : Constants.FAILED);

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

                    switch (GetProperty("BizTalk360_expectedStateSendPorts", "Do not monitor"))
                    {
                        case "Started": { expectedStateSendPorts = 2; break; }
                        case "Stopped": { expectedStateSendPorts = 3; break; }
                        case "Bound": { expectedStateSendPorts = 0; break; }
                        case "Do not monitor": { expectedStateSendPorts = 4; break; }
                        default: { expectedStateSendPorts = 4; break; }
                    }

                    // Don't add mappings if the Send Ports don't have to be monitored
                    if (expectedStateSendPorts != 4)
                    {
                        // Create Send Port mappings to the Alert
                        Console.Write("- Adding {0} Send Port(s) to alert: ", sendPorts.Count);

                        request = CreateSendPortMappings(sendPorts, expectedStateSendPorts);
                        success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                        Console.WriteLine("{0}", success == true ? Constants.SUCCESS : Constants.FAILED);

                        if (!success) { return false; }
                    }
                    else
                    {
                        Console.WriteLine("- Send Port monitoring not configured");
                    }
                }

                // If the BizTalk Application contains Orchestrations or Send Ports, create Alert Mappings for Suspended Instances
                if ((orchestrations.Count > 0 && expectedStateOrchestrations != 4) || (sendPorts.Count > 0 && expectedStateSendPorts !=4))
                {
                    Console.Write("- Adding Instance Warning/Error Levels to alert: ");

                    // Create Instance mappings to the Alert
                    request = CreateServiceInstanceMappings();
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                    Console.WriteLine("{0}", success == true ? Constants.SUCCESS : Constants.FAILED);

                    if (!success) { return false; }
                }
                else
                {
                    Console.WriteLine("- Instance State monitoring not required");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(Constants.FAILED);
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
            webRequest.Credentials = new NetworkCredential(BizTalk360User, BizTalk360UserPassword);
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

                // If the BizTalk Application contains no Receive Ports, Orchestrations or Send Ports, move on to the next BizTalk Application
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
