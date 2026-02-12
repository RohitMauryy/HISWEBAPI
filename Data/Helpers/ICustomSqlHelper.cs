using System.Data;
using Microsoft.Data.SqlClient;

namespace HISWEBAPI.Data.Helpers
{
    public interface ICustomSqlHelper
    {
        // DML Methods
        dynamic DML(SqlTransaction tnx, SqlConnection con, string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        dynamic DML(SqlTransaction tnx, string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        dynamic DML(string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);

        // ExecuteScalar Methods
        dynamic ExecuteScalar(string sqlCmd, object inParameters);
        dynamic ExecuteScalar(string sqlCmd, CommandType commandType, object inParameters);
        dynamic ExecuteScalar(SqlTransaction tnx, string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        dynamic ExecuteScalar(string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);

        // GetDataTable Methods
        DataTable GetDataTable(string sqlCmd, CommandType commandType, object inParameters = null);
        DataTable GetDataTable(SqlTransaction tnx, string sqlCmd, CommandType commandType, object inParameters = null);

        // GetDataSet Methods
        DataSet GetDataSet(string sqlCmd, CommandType commandType, object inParameters = null);
        DataSet GetDataSet(string sqlCmd, CommandType commandType, object inParameters = null, string TableName = "");

        // RunProcedure Methods
        string RunProcedureRetInsert(string storedProcName, IDataParameter[] parameters);
        DataSet RunProcedure(string storedProcName, IDataParameter[] parameters, string tableName);
        DataSet RunProcedure(string storedProcName, string tableName);
        long RunProcedureInsert(string storedProcName, IDataParameter[] parameters);
        void RunProcedure(string storedProcName, SqlParameter[] parameters);
        SqlConnection? GetConnectionString();
    }
}