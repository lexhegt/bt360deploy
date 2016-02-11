using System;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains some fields which must be provided with each call to the BizTalk360 API
    /// </summary>
    public class EnvironmentSettings
    {
        public Guid id { get; set; }
        public int licenseEdition { get; set; }
    }
}
