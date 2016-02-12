// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
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
