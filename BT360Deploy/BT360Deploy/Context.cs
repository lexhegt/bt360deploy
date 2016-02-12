// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
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
