using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxonOlympus.BT360Deploy
{
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
}
