using System;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the fields to be able to delete a BizTalk360 Alert
    /// </summary>
    public class DeleteUserAlertRequest
    {
        public Context context { get; set; }
        public String alarmName { get; set; }
    }
}
