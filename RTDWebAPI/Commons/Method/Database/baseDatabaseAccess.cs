using Oracle.ManagedDataAccess;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace demoWebAPI.Models
{
    public class baseDatabaseAccess
    {
        public string connString = "";
        public OracleConnection oracleConnection = null;

        public virtual void Start() { }
        //Insert into {(COLUMN)}  VALUES (VALUE) , Insert into {(COLUMN)}  VALUES (VALUE),(VALUE)
        public virtual bool InsertInto() { return true; }
        public virtual DataSet SelectToDataSet() { DataSet ds = new DataSet();  return ds; }
        //Select COLUMN FROM TABLE WHERE
        public virtual bool UpdateTable(DataSet ds) { return true; }
        //Update TABLE SET COLUMN=VALUE WHERE ...........
        public virtual bool DeleteSyntax(string SQLSyntax) { return true; }
        //Delete TABLE WHERE ...........
        public virtual bool SQLSentence(string SQLSyntax) { return true; }
        //SQL Syntax
        //SQL TRANSATIONS
        //BEGIN ... SAVEPOINT; ... COMMIT; END; ROLLBACK [TO SAVEPOINT] ;
    }
}