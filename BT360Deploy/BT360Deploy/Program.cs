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
using CommandLine;

namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// This console application enables you to create BizTalk360 Alerts
    /// It supports the Deployment Framework (BTDF)
    /// </summary>
    class Program
    {
        static private BizTalkApplication bizTalkapplication;
        static private ReceivePortCollection receivePorts;
        static private OrchestrationCollection orchestrations;
        static private SendPortCollection sendPorts;
        static private XmlDocument xmlDocument = new XmlDocument();
        static private Options options = new Options();

        static private string environmentId = "";
        static private string baseUrl = "";
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

            Console.WriteLine("\r\nBT360Deploy v{0}.{1} - Creation of BizTalk360 Alerts\r\n", Assembly.GetEntryAssembly().GetName().Version.Major, Assembly.GetEntryAssembly().GetName().Version.Minor);

            // Get command line parameters
            Console.WriteLine("Get parameters");
            CommandLine.Parser.Default.ParseArguments(args, options);
        
            // Exit in case no parameters were passed
            if (string.IsNullOrEmpty(options.BizTalkApplication) || (string.IsNullOrEmpty(options.SettingsFile)))
            {
                Console.WriteLine("Pass parameters:\r\n -a <Name of BizTalk Application>\r\n -s <Name and location of BTDF Settings file>");
                return;
            }

            Console.WriteLine("- Name BizTalk Application: {0}", options.BizTalkApplication);
            Console.WriteLine("- Settings File: {0}", options.SettingsFile);

            try
            {

                // Load settings file
                Boolean success = LoadSettingsFile(options.SettingsFile);
                if (!success) return;

                Console.WriteLine("- BizTalk360 URL: {0}", baseUrl);
                Console.WriteLine("- BizTalk360 User: {0}\r\n", BizTalk360User);

                // Check BizTalk360 url
                CheckBizTalk360URL();

                // todo: Check credentials
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
                Console.WriteLine("\r\nException: {0}", ex.Message);
                return;
            }
            
            Console.WriteLine("\r\nFinished creating alert '{0}'", options.BizTalkApplication);
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
                    Console.WriteLine("FAILED");
                    throw new Exception("EnvironmentID invalid or not found.");
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(urlParameters).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Do something with result
                    var getBizTalk360InfoResponse = response.Content.ReadAsAsync<GetBizTalk360InfoResponse>().Result;
                    if (getBizTalk360InfoResponse.success && getBizTalk360InfoResponse.errors == null)
                    {
                        Console.WriteLine("SUCCESS");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("{0} ({1})", getBizTalk360InfoResponse.errors[0].errorCode, getBizTalk360InfoResponse.errors[0].description);
                    }
                }
                else
                {
                    Console.WriteLine("FAILED");
                    throw new Exception(String.Format("{0}", response.ReasonPhrase));
                }
                return;
            }
        }

        /// <summary>
        /// Checks if the provided User Profile is a valid BizTalk360 User Profile
        /// </summary>
        static private void CheckUserProfile()
        {
            Console.Write("\r\nCheck BizTalk360 User Profile: ");

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
                    Console.WriteLine("FAILED");
                    throw new Exception("EnvironmentID invalid or not found.");
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(urlParameters).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Do something with result
                    var getUserProfileResponse = response.Content.ReadAsAsync<GetUserProfileResponse>().Result;
                    if (getUserProfileResponse.success && getUserProfileResponse.errors == null)
                    {
                        Console.WriteLine("SUCCESS");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("{0} ({1})", getUserProfileResponse.errors[0].errorCode, getUserProfileResponse.errors[0].description);
                    }
                }
                else
                {
                    Console.WriteLine("FAILED");
                    throw new Exception(String.Format("{0}", response.ReasonPhrase));
                }
                return;
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
                //alertASAPErrorDetectionCount = GetProperty("alertASAPErrorDetectionCount", 0),
                isContinuousErrorRestricted = GetProperty("BizTalk360_isContinuousErrorRestricted", true),
                continuousErrorMaxCount = GetProperty("BizTalk360_continuousErrorMaxCount", 3),
                isAlertOnCorrection = GetProperty("BizTalk360_isAlertOnCorrection", true),
                isAlertASAP = GetProperty("BizTalk360_isAlertASAP", true),
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
                isAlertRestEndpointEnabled = false,
                createdBy = ""
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
                Console.Write("\r\nCreating alert '{0}': ", options.BizTalkApplication);

                string userAlertRequest = CreateCreateUserAlertRequest();
                success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/CreateUserAlarm", baseUrl), "POST", userAlertRequest);

                Console.WriteLine("{0}", success == true ? "SUCCESS" : "FAILED");
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED");
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
                    Console.Write("\r\nDeleting existing alert '{0}': ", alertName);

                    string deleteUserAlertRequest = CreateDeleteUserAlertRequest(alertName);
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/DeleteUserAlarm", baseUrl), "POST", deleteUserAlertRequest);

                    Console.WriteLine("{0}", success == true ? "SUCCESS" : "FAILED");
                    return (success);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED");
                throw ex;
            }
            return (true);
        }

        /// <summary>
        /// Retrieves a BizTalk Application to be able to retrieve its artifacts
        /// </summary>
        /// <param name="bizTalkApplication">Name of the BizTalk Application which has to be retrieved</param>
        /// <returns>An object containing the BizTAlk Application</returns>
        static private BizTalkApplication GetBizTalkApplication(string bizTalkApplication)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("BizTalkGroupService.svc", "GetBizTalkApplication"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    return (null);
                }

                string urlParameters = string.Format("?environmentId={0}&applicationName={1}", environmentId, bizTalkApplication);

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(urlParameters).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Do something with result
                    var getBizTalkApplicationResponse = response.Content.ReadAsAsync<GetBizTalkApplicationResponse>().Result;
                    if (getBizTalkApplicationResponse.success && getBizTalkApplicationResponse.errors == null)
                    {
                        // Return the result.
                        return getBizTalkApplicationResponse.bizTalkApplication;
                    }
                    else
                    {
                        Console.WriteLine("{0} ({1})", getBizTalkApplicationResponse.errors[0].errorCode, getBizTalkApplicationResponse.errors[0].description);
                    }
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves a collection containing the Orchestrations of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Orchestrations have to be retrieved</param>
        /// <returns>The collection of Orchestrations for the given BizTalk Application</returns>
        static private OrchestrationCollection GetOrchestrations(BizTalkApplication biztalkApplication)
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

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}&applicationName={1}", urlParameters, biztalkApplication.name)).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Do something with result
                    var getOrchestrationsResponse = response.Content.ReadAsAsync<GetOrchestrationsResponse>().Result;
                    if (getOrchestrationsResponse.success)
                    {
                        // Return the result.
                        return getOrchestrationsResponse.orchestrations;
                    }
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
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
                //DateTime dt = new DateTime(1, 1, 1, dateTime.Hour, dateTime.Minute, 0).AddTicks(117780000000);

                Int32 unixTimestamp = (Int32)(DateTime.Parse("1/12/1968 " + dateTime.Hour + ":" + dateTime.Minute).ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                return "/Date(" + unixTimestamp + "000+0000)/";
            }
            catch (Exception)
            { }
            return (def.Add(new TimeSpan(1, 0, 0)).Ticks.ToString());
        }

        /// <summary>
        /// Retrieves a collection containing the Receive Ports of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Receive Ports have to be retrieved</param>
        /// <returns>The collection of Receive Ports for the given BizTalk Application</returns>
        static private ReceivePortCollection GetReceivePorts(BizTalkApplication biztalkApplication)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;

            using (HttpClient httpClient = new HttpClient(handler))
            {
                // Construct the base address of the service.
                httpClient.BaseAddress = new Uri(ConstructServiceURL("BizTalkApplicationService.svc", "GetReceivePorts"));

                // Add an Accept header for JSON format.
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Retrieve the environment Id
                //string environmentId = GetEnvironmentId();

                // Check if environmentId is populated
                if (string.IsNullOrEmpty(environmentId))
                {
                    // Unable to retrieve the current environment
                    return (null);
                }

                string urlParameters = string.Format("?environmentId={0}", environmentId);

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}&applicationName={1}", urlParameters, biztalkApplication.name)).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Do something with result
                    var getReceivePortsResponse = response.Content.ReadAsAsync<GetReceivePortsResponse>().Result;
                    if (getReceivePortsResponse.success)
                    {
                        // Return the result.
                        return getReceivePortsResponse.receivePorts;
                    }
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieves a collection containing the Send Ports of a given BizTalk Application
        /// </summary>
        /// <param name="biztalkApplication">The BizTalk Application for which the Send Ports have to be retrieved</param>
        /// <returns>The collection of Send Ports for the given BizTalk Application</returns>
        static private SendPortCollection GetSendPorts(BizTalkApplication biztalkApplication)
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

                // List data response.
                HttpResponseMessage response = httpClient.GetAsync(string.Format("{0}&applicationName={1}", urlParameters, biztalkApplication.name)).Result;
                if (response.IsSuccessStatusCode)
                {
                    // Do something with result
                    var getSendPortsResponse = response.Content.ReadAsAsync<GetSendPortsResponse>().Result;
                    if (getSendPortsResponse.success)
                    {
                        // Return the result.
                        return getSendPortsResponse.sendPorts;
                    }
                }
                else
                {
                    Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
                }
                return null;
            }
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
                    // Do something with result
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
                Console.Write("\r\nLoad settings file: ");
                xmlDocument.Load(settingsFile);

                environmentId = xmlDocument.SelectSingleNode("/settings/property[@name='BizTalk360_environmentId']").InnerText;
                baseUrl = xmlDocument.SelectSingleNode("/settings/property[@name='BizTalk360_url']").InnerText;
                BizTalk360User = xmlDocument.SelectSingleNode("/settings/property[@name='BizTalk360_user']").InnerText;
                BizTalk360UserPassword = xmlDocument.SelectSingleNode("/settings/property[@name='BizTalk360_userPassword']").InnerText;

            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED\r\n");
                Console.WriteLine(ex.Message);
                Console.WriteLine("\r\nPlease provide a correct settings file. The settings file should at least contain the following parameters:");
                Console.WriteLine("- BizTalk360_environmentId");
                Console.WriteLine("- BizTalk360_url");
                Console.WriteLine("- BizTalk360_user");
                Console.WriteLine("- BizTalk360_userPassword");

                return false;
            }

            Console.WriteLine("SUCCESS");
            return true;
        }

        /// <summary>
        /// Creates mappings for different kind of artifacts and adds them to the created BizTalk360 Alert
        /// </summary>
        /// <returns></returns>
        static private Boolean ManageAlertMonitorConfig()
        {
            int expectedState = 0;
            string request = "";
            Boolean success = false;

            Console.WriteLine("\r\nAdd mappings to alert '{0}'", options.BizTalkApplication);

            try
            {
                // If the BizTalk Application contains Receive Ports, create Alert Mappings
                if (receivePorts.Count > 0)
                {
                    switch (GetProperty("BizTalk360_expectedStateReceiveLocations", "Enabled"))
                    {
                        case "Enabled": { expectedState = 0; break; }
                        case "Disabled": { expectedState = 1; break; }
                        case "Do not monitor": { expectedState = 2; break; }
                    }

                    // Create Receive Ports mappings to the Alert
                    Console.Write("- Adding {0} Receive Port(s) to alert: ", receivePorts.Count);

                    request = CreateReceivePortsMappings(receivePorts, expectedState);
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                    Console.WriteLine("{0}", success == true ? "SUCCESS" : "FAILED");

                    if (!success) { return false; }
                }

                expectedState = 0;

                // If the BizTalk Application contains Orchestrations, create Alert Mappings
                if (orchestrations.Count > 0)
                {
                    switch (GetProperty("BizTalk360_expectedStateOrchestrations", "Started"))
                    {
                        case "Unbound": { expectedState = 1; break; }
                        case "Started": { expectedState = 2; break; }
                        case "Stopped": { expectedState = 3; break; }
                        case "Unenlisted": { expectedState = 0; break; }
                        case "Do not monitor": { expectedState = 4; break; }
                    }

                    // Create Orchestration mappings to the Alert
                    Console.Write("- Adding {0} Orchestration(s) to alert: ", orchestrations.Count);

                    request = CreateOrchestrationMappings(orchestrations, expectedState);
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                    Console.WriteLine("{0}", success == true ? "SUCCESS" : "FAILED");

                    if (!success) { return false; }
                }

                expectedState = 0;

                // If the BizTalk Application contains Send Ports, create Alert Mappings
                if (sendPorts.Count > 0)
                {

                    switch (GetProperty("BizTalk360_expectedStateSendPorts", "Started"))
                    {
                        case "Started": { expectedState = 2; break; }
                        case "Stopped": { expectedState = 3; break; }
                        case "Unenlisted": { expectedState = 0; break; }
                        case "Do not monitor": { expectedState = 1; break; }
                    }

                    // Create Send Port mappings to the Alert
                    Console.Write("- Adding {0} Send Port(s) to alert: ", sendPorts.Count);

                    request = CreateSendPortMappings(sendPorts, expectedState);
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                    Console.WriteLine("{0}", success == true ? "SUCCESS" : "FAILED");

                    if (!success) { return false; }
                }

                // If the BizTalk Application contains Orchestrations or Send Ports, create Alert Mappings for Suspended Instances
                if (orchestrations.Count > 0 || sendPorts.Count > 0)
                {
                    Console.Write("- Adding Instance Warning/Error Levels to alert: ");

                    // Create Instance mappings to the Alert
                    request = CreateServiceInstanceMappings();
                    success = ProcessResponse(String.Format("{0}/Services.REST/AlertService.svc/ManageAlertMonitorConfig", baseUrl), "POST", request);

                    Console.WriteLine("{0}", success == true ? "SUCCESS" : "FAILED");

                    if (!success) { return false; }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED");
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

            Console.WriteLine("\r\nRetrieve information about the BizTalk Application");

            try
            {
                // Get the BizTalk Application
                Console.WriteLine("- BizTalk Application");
                bizTalkapplication = GetBizTalkApplication(options.BizTalkApplication);

                if (bizTalkapplication == null)
                { 
                    // BizTalk Application not found
                    throw new Exception(String.Format("BizTalk Application '{0}' not found!", options.BizTalkApplication));
                }
                
                // Get all Receive Ports
                Console.WriteLine("- Receive Ports");
                receivePorts = GetReceivePorts(bizTalkapplication);

                // Get all Orchestrations
                Console.WriteLine("- Orchestrations");
                orchestrations = GetOrchestrations(bizTalkapplication);

                // Get all Send Ports
                Console.WriteLine("- Send Ports");
                sendPorts = GetSendPorts(bizTalkapplication);

                // If the BizTalk Application contains no Receive Ports, Orchestrations or Send Ports, move on to the next BizTalk Application
                if (receivePorts.Count == 0 && orchestrations.Count == 0 && sendPorts.Count == 0)
                {
                    throw new Exception((String.Format("No Receive/Send Ports and Orchestrations found => no alert created\r\n\r\n")));
                    //return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }
    }
    /// <summary>
    /// Class which contains the fields of BizTalk360 Alerts
    /// </summary>
    public class UserAlertRequest
    {
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
    /// <summary>
    /// Class which contains the fields for all possible Alert mappings
    /// </summary>
    public class AlertMappingRequest
    {
        public Context context { get; set; }
        public Int16 operation { get; set; }
        public string monitorGroupName { get; set; }
        public string monitorGroupType { get; set; }
        public string monitorName { get; set; }
        public string alarmName { get; set; }
        public string serializedMonitorConfigforApplicationServiceInstance { get; set; }
        public string serializedMonitorConfigforApplicationOrchestration { get; set; }
        public string serializedMonitorConfigforApplicationReceiveLocation { get; set; }
        public string serializedMonitorConfigforApplicationSendPorts { get; set; }
        public string serializedMonitorConfigforBizTalkServerDisks { get; set; }
        public string serializedMonitorConfigforBizTalkSystemResources { get; set; }
        public string serializedMonitorConfigforBizTalkEventLogs { get; set; }
        public string serializedMonitorConfigforBizTalkNTServices { get; set; }
        public string serializedMonitorConfigforSQLServerDisks { get; set; }
        public string serializedMonitorConfigforSQLSystemResources { get; set; }
        public string serializedMonitorConfigforSQLEventLogs { get; set; }
        public string serializedMonitorConfigforSQLNTServices { get; set; }
        public string serializedMonitorConfigforSQLInstanceJobs { get; set; }
        public string serializedMonitorConfigforHostInstances { get; set; }
        public string serializedMonitorConfigforWebEndPoints { get; set; }
        public string serializedMonitorConfigforDatabaseQuery { get; set; }
        public string serializedMonitorConfigforMessageBoxViewer { get; set; }
        public string serializedJsonMonitorConfig { get; set; }
    }
    /// <summary>
    /// Class which contains fields which has to be provided with each call to the BizTalk360 API
    /// </summary>
    public class Context
    {
        public string callerReference { get; set; }
        public EnvironmentSettings environmentSettings { get; set; }
    }
    /// <summary>
    /// Class which contains all the days of the week
    /// This must be provided while creating a BizTalk360 Alert
    /// </summary>
    public class DaysOfWeek
    {
        public DaysOfWeek(){}
        public DaysOfWeek(string values)
        {
            // Fri = false, Mon = false, Sat = false, Sun = false, Thu = false, Tue = false, Wed = false
            string[] days = values.Split(',');

            foreach (string day in days)
            {
                switch (day.Trim().Substring(0,3).ToLower())
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
    /// <summary>
    /// Class which contains the fields to be able to delete a BizTalk360 Alert
    /// </summary>
    public class DeleteUserAlertRequest
    {
        public Context context { get; set; }
        public String alarmName { get; set; }
    }
    /// <summary>
    /// Class which contains some fields which must be provided with each call to the BizTalk360 API
    /// </summary>
    public class EnvironmentSettings
    {
        public Guid id { get; set; }
        public int licenseEdition { get; set; }
    }
    /// <summary>
    /// Class which is part of the response of call to the BizTalk360 API
    /// </summary>
    public class Errors
    {
        public string stackTrace { get; set; }
        public string description { get; set; }
    }
    /// <summary>
    /// Class which contains the arguments which were supplied at the command line
    /// </summary>
    public class Options
    {
        [Option('a', "application", HelpText = "Name of the BizTalk Application for which an alert will be created")]
        public string BizTalkApplication { get; set; }
        [Option('s', "settingsfile", HelpText = "Deployment Framework file which contains the settings for the alert")]
        public string SettingsFile { get; set; }
    }
    /// <summary>
    /// Class which contains the fields to be able to create a BizTalk360 Alert
    /// </summary>
    public class Request
    {
        public Context context { get; set; }
        public UserAlertRequest alarm { get; set; }
    }
    /// <summary>
    /// Class which contains the fields from a response message of the BizTalk360 API
    /// </summary>
    public class Response
    {
        public Boolean success { get; set; }
        public List<Errors> errors { get; set; }
    }
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
