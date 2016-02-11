using CommandLine;

namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains the arguments which were supplied at the command line
    /// </summary>
    public class Options
    {
        [Option('a', "application", HelpText = "Name of the BizTalk Application for which an alert will be created")]
        public string BizTalkApplication { get; set; }
        [Option('s', "settingsfile", HelpText = "Deployment Framework file which contains the settings for the alert")]
        public string SettingsFile { get; set; }
    }
}
