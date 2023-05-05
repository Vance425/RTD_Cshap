namespace RTDWebAPI.Commons.DataRelated.SQLSentence
{
    public class SqlSentences
    {
        public string _ABC { get; }
        private static string _getAssociatebyCarrierID = "select * from ads_info";
        public string getAssociatebyCarrierID
        {
            get { return _getAssociatebyCarrierID; }
        }
        public string _getAssociatebyLotID(string LotID)
        {
            string SQL = "select * from ads_info where lotid = '{0}'";
            SQL = string.Format(SQL, LotID);
            return SQL;
        }
    }
}
