namespace GyroSystemControl
{
    public class ClassGyroSystemControl
    {
        private static string CurrentVersion()
        {
            string _version = "1.0.25.0224.1.0.1";
            return string.Format("Version {0}", _version);
        }

        public string GetSystemVersion
        {
            get { return CurrentVersion(); }
        }
    }
}