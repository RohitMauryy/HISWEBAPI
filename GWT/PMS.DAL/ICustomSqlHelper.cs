using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace PMS.DAL
{
    public interface ICustomSqlHelper
    {
        // DML Methods
        dynamic DML(SqlTransaction tnx, SqlConnection con, string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        dynamic DML(SqlTransaction tnx, string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        dynamic DML(string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        Task<dynamic> DMLAsync(string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);

        // ExecuteScalar Methods
        dynamic ExecuteScalar(string sqlCmd, object inParameters);
        dynamic ExecuteScalar(string sqlCmd, CommandType commandType, object inParameters);
        dynamic ExecuteScalar(SqlTransaction tnx, string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        dynamic ExecuteScalar(string sqlCmd, CommandType commandType, object inParameters = null, object outParameters = null);
        Task<dynamic> ExecuteScalarAsync(string sqlCmd, CommandType commandType, object inParameters = null);

        // GetDataTable Methods
        DataTable GetDataTable(string sqlCmd, CommandType commandType, object inParameters = null);
        DataTable GetDataTable(SqlTransaction tnx, string sqlCmd, CommandType commandType, object inParameters = null);
        Task<DataTable> GetDataTableAsync(string sqlCmd, CommandType commandType, object inParameters = null);

        // GetDataSet Methods
        DataSet GetDataSet(string sqlCmd, CommandType commandType, object inParameters = null);
        DataSet GetDataSet(string sqlCmd, CommandType commandType, object inParameters = null, string TableName = "");
        Task<DataSet> GetDataSetAsync(string sqlCmd, CommandType commandType, object inParameters = null);
        Task<DataSet> GetDataSetAsync(string sqlCmd, CommandType commandType, object inParameters = null, string TableName = "");

        // RunProcedure Methods
        string RunProcedureRetInsert(string storedProcName, IDataParameter[] parameters);
        Task<string> RunProcedureRetInsertAsync(string storedProcName, IDataParameter[] parameters);

        DataSet RunProcedure(string storedProcName, IDataParameter[] parameters, string tableName);
        DataSet RunProcedure(string storedProcName, string tableName);
        Task<DataSet> RunProcedureAsync(string storedProcName, IDataParameter[] parameters, string tableName);
        Task<DataSet> RunProcedureAsync(string storedProcName, string tableName);

        Int64 RunProcedureInsert(string storedProcName, IDataParameter[] parameters);
        Task<Int64> RunProcedureInsertAsync(string storedProcName, IDataParameter[] parameters);
    }
}