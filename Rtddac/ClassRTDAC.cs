namespace RTDDAC
{
    public class ClassRTDAC
    {
        private static string CurrentVersion()
        {
            string _version = "1.0.24.1.0.0";
            return string.Format("Version {0}", _version);
        }

        public string GetRTDACVersion
        {
            get { return CurrentVersion(); }
        }
    }
}