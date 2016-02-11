namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the fields to be able to create a BizTalk360 Alert
    /// </summary>
    public class Request
    {
        public Context context { get; set; }
        public UserAlertRequest alarm { get; set; }
    }
}
