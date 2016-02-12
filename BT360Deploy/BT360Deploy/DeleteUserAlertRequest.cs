// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the fields to be able to delete a BizTalk360 Alert
    /// </summary>
    public class DeleteUserAlertRequest
    {
        public Context context { get; set; }
        public string alarmName { get; set; }
    }
}
