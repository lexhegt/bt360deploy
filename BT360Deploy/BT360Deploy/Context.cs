namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains fields which has to be provided with each call to the BizTalk360 API
    /// </summary>
    public class Context
    {
        public string callerReference { get; set; }
        public EnvironmentSettings environmentSettings { get; set; }
    }
}
