using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace DataAccess
{
    public class SqlHelper
    {
        const string DefaultConnectionString = @"Server=.\SQLEXPRESS01;Database=PatientMonitorDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;";
        private readonly string _connectionString;

        public SqlHelper(string connectionString)
        {
            _connectionString = connectionString ?? DefaultConnectionString;
        }

        public DataTable Select(string sql)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }
        public DataTable Select(string sql, string where, string order)
        {
            if (!string.IsNullOrEmpty(where))
            {
                sql += " WHERE " + where;
            }
            if (!string.IsNullOrEmpty(order))
            {
                sql += " ORDER BY " + order;
            }
            return Select(sql);
        }

        public DataRow? FindById(string tableName, object idValue, string idColumn = "ID")
        {
            var sql = $"SELECT * FROM {tableName} WHERE {idColumn}='{idValue}'";
            var dt = Select(sql);
            if (dt != null && dt.Rows.Count > 0)
                return dt.Rows[0];

            return null;
        }

        public static T FromDataRow<T>(DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));
            var obj = Activator.CreateInstance<T>();
            foreach (var prop in typeof(T).GetProperties())
            {
                if (row.Table.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                {
                    prop.SetValue(obj, Convert.ChangeType(row[prop.Name], prop.PropertyType));
                }
            }
            return obj;
        }

        public int ExecuteNonQuery(string sql)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteScalar(string sql)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }

        private string CreateInsertSql<T>(T obj, string tableName = null)
        {
            tableName ??= typeof(T).Name;

            var properties = typeof(T).GetProperties();
            var columns = new List<string>();
            var values = new List<string>();
            foreach (var prop in properties)
            {
                columns.Add(prop.Name);
                var value = prop.GetValue(obj);
                if (value is string || value is DateTime)
                {
                    values.Add($"'{value}'");
                }
                else
                {
                    values.Add(value?.ToString() ?? "NULL");
                }
            }
            var columnsJoined = string.Join(", ", columns);
            var valuesJoined = string.Join(", ", values);
            return $"INSERT INTO {tableName} ({columnsJoined}) VALUES ({valuesJoined})";
        }

        private string CreateUpdateSql<T>(T obj, string tableName = null, string idColumn = "ID")
        {
            tableName ??= typeof(T).Name;
            var properties = typeof(T).GetProperties();
            var setClauses = new List<string>();
            object? idValue = null;
            foreach (var prop in properties)
            {
                if (prop.Name.Equals(idColumn, StringComparison.OrdinalIgnoreCase))
                {
                    idValue = prop.GetValue(obj);
                    continue;
                }
                var value = prop.GetValue(obj);
                if (value is string || value is DateTime)
                {
                    setClauses.Add($"{prop.Name}='{value}'");
                }
                else
                {
                    setClauses.Add($"{prop.Name}={value?.ToString() ?? "NULL"}");
                }
            }
            if (idValue == null)
            {
                throw new ArgumentException($"The object must have a property named '{idColumn}' with a non-null value.");
            }
            var setClauseJoined = string.Join(", ", setClauses);
            return $"UPDATE {tableName} SET {setClauseJoined} WHERE {idColumn}='{idValue}'";
        }

        private string CreateDeleteSql<T>(T obj, string tableName = null, string idColumn = "ID")
        {
            tableName ??= typeof(T).Name;
            var idProperty = typeof(T).GetProperty(idColumn);
            if (idProperty == null)
            {
                throw new ArgumentException($"The object must have a property named '{idColumn}'.");
            }
            var idValue = idProperty.GetValue(obj);
            if (idValue == null)
            {
                throw new ArgumentException($"The property '{idColumn}' must have a non-null value.");
            }
            return $"DELETE FROM {tableName} WHERE {idColumn}='{idValue}'";
        }

        public int Insert<T>(T obj, string tableName = null)
        {
            var sql = CreateInsertSql(obj, tableName);
            return ExecuteNonQuery(sql);
        }

        public int Update<T>(T obj, string tableName = null, string idColumn = "ID")
        {
            var sql = CreateUpdateSql(obj, tableName, idColumn);
            return ExecuteNonQuery(sql);
        }

        public int Delete<T>(T obj, string tableName = null, string idColumn = "ID")
        {
            var sql = CreateDeleteSql(obj, tableName, idColumn);
            return ExecuteNonQuery(sql);
        }
    }
}