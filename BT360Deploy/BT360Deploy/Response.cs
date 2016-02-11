using System;
using System.Collections.Generic;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the fields from a response message of the BizTalk360 API
    /// </summary>
    public class Response
    {
        public Boolean success { get; set; }
        public List<Errors> errors { get; set; }
    }
}
