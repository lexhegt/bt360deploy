// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
using CommandLine;

namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the arguments which were supplied at the command line
    /// </summary>
    public class Options
    {
        public Options()
        {
            Existing = "OVERWRITE";
        }
        [Option('a', "application", HelpText = "Name of the BizTalk Application for which an alert will be created")]
        public string BizTalkApplication { get; set; }
        [Option('e', "existing", HelpText = "Action on existing alarm: [overwrite]/[update]/[donothing]")]
        public string Existing { get; set; }
        [Option('s', "settingsfile", HelpText = "Deployment Framework file which contains the settings for the alert")]
        public string SettingsFile { get; set; }
        [Option('u', "undeploy", HelpText = "Delete the alert")]
        public bool Undeploy { get; set;}
    }
}
