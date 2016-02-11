namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which is part of the response of call to the BizTalk360 API
    /// </summary>
    public class Errors
    {
        public string stackTrace { get; set; }
        public string description { get; set; }
    }
}
