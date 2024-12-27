namespace GyroLibrary.GyroSystemControl
{
    public class ClassGyroSystemControl
    {
        private static string CurrentVersion()
        {
            string _version = "1.0.25.0224.1.0.1";  ///主版本號(1.0).年份.日期.子版本號. Bug 修正.新增/修改/刪除
            return string.Format("Version {0}", _version);
        }

        public string GetSystemVersion
        {
            get { return CurrentVersion(); }
        }
    }
}