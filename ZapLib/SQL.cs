﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Reflection;
using ZapLib.Utility;

namespace ZapLib
{
    /// <summary>
    /// SQL Server 連線查詢輔助工具
    /// </summary>
    public class SQL
    {
        /// <summary>
        /// 是否已經與資料庫連線成功
        /// </summary>
        public bool IsConn { get; private set; } = false;
        /// <summary>
        /// 目前使用的資料庫交易物件 (需先啟用 transaction)
        /// </summary>
        public SqlTransaction Tran { get; private set; }
        /// <summary>
        /// 目前使用的資料庫命令物件
        /// </summary>
        public SqlCommand Cmd { get; private set; }

        /// <summary>
        /// 資料庫連線逾時秒數，預設 15 秒
        /// </summary>
        public int Timeout { get; set; } = 15;
        /// <summary>
        /// 資料庫已經安裝憑證，是否對資料進行 SSL 加密，預設 false
        /// </summary>
        public bool Encrypt { get; set; } = false;
        /// <summary>
        /// 資料庫工作負載類型，預設為 ReadWrite
        /// </summary>
        public string ApplicationIntent { get; set; } = "ReadWrite";
        /// <summary>
        /// 資料庫是否在不同子網路上的可用性群組，預設為 false
        /// </summary>
        public bool MultiSubnetFailover { get; set; } = false;
        /// <summary>
        /// 資料庫傳輸通道使否開啟 SSL 加密，預設為 false
        /// </summary>
        public bool TrustServerCertificate { get; set; } = false;


        private string connString;
        private SqlConnection Conn = null;
        private bool isTran = false;
        private MyLog log;
        private List<string> errormessage;


        /// <summary>
        /// 初始化 SQL 連線物件，將使用 .config 中的資料庫連線資訊進行連線
        /// </summary>
        /// <param name="transaction">是否開啟 transaction</param>
        public SQL(bool transaction = false)
        {
            string DBName = Config.Get("DBName"),
                   DBHost = Config.Get("DBHost"),
                   DBAct = Config.Get("DBAct"),
                   DBPwd = Config.Get("DBPwd"),
                   template = "Server={0};Database={1};User ID={2};Password={3}",
                   basestring = string.Format(template, DBHost, DBName, DBAct, DBPwd);
            isTran = transaction;
            connString = buildconnString(basestring);
            log = new MyLog();
            log.SilentMode = Config.Get("SilentMode");
            errormessage = new List<string>();
        }

        /// <summary>
        /// 初始化 SQL 連線物件，將使用指定的連線資訊進行連線
        /// </summary>
        /// <param name="dbHost">資料庫位置</param>
        /// <param name="dbName">資料庫名稱</param>
        /// <param name="dbAct">連線帳號</param>
        /// <param name="dbPwd">連線密碼</param>
        /// <param name="transaction">是否開啟 transaction</param>
        public SQL(string dbHost, string dbName, string dbAct, string dbPwd, bool transaction = false)
        {
            string template = "Server={0};Database={1};User ID={2};Password={3}",
                   basestring = string.Format(template, dbHost, dbName, dbAct, dbPwd);
            isTran = transaction;
            connString = buildconnString(basestring);
            log = new MyLog();
            log.SilentMode = Config.Get("SilentMode");
            errormessage = new List<string>();
        }

        /// <summary>
        /// 初始化 SQL 連線物件，嘗試從 .config 中抓取指定名稱的連線字串或 直接給與連線字串 進行連線
        /// </summary>
        /// <param name="connectionString">指定的連線字串名稱 或 連線字串</param>
        public SQL(string connectionString)
        {
            connString = Config.GetConnectionString(connectionString) ?? connectionString;
            log = new MyLog();
            log.SilentMode = Config.Get("SilentMode");
            errormessage = new List<string>();
        }

        /// <summary>
        /// 取得所有資料庫的錯誤訊息
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage() => string.Join("\n", errormessage);

        /// <summary>
        /// 取得主要的資料庫連線物件
        /// </summary>
        /// <returns></returns>
        public SqlConnection GetConnection() => Conn;

        /// <summary>
        /// 手動連線資料庫，可以使用 IsConn 來確認使否連線成功
        /// </summary>
        public void Connet()
        {
            try
            {
                Conn = new SqlConnection(connString);
                Conn.Open();

                Cmd = new SqlCommand();
                Cmd.Connection = Conn;
                IsConn = true;

                if (isTran)
                {
                    Tran = Conn.BeginTransaction();
                    Cmd.Transaction = Tran;
                }
            }
            catch (Exception e)
            {
                log.Write(e.ToString());
                errormessage.Add(e.ToString());
                Conn = null;
            }
        }

        private string buildconnString(string s) => (s += $";Connect Timeout={Timeout};Encrypt={Encrypt};TrustServerCertificate={TrustServerCertificate};ApplicationIntent={ApplicationIntent};MultiSubnetFailover={MultiSubnetFailover}");

        /// <summary>
        /// 手動執行查詢命令，需自行控制可能發生的錯誤
        /// </summary>
        /// <param name="sql">查詢語法</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <returns>返回 SqlDataReader 物件可自行控制</returns>
        public SqlDataReader Query(string sql, object param = null)
        {
            Cmd.CommandText = sql;
            Cmd.CommandType = CommandType.Text;
            Cmd.Parameters.Clear();
            setParaInput(Cmd, param);
            return Cmd.ExecuteReader();
        }

        /// <summary>
        /// 手動執行預存程序，需自行控制可能發生的錯誤
        /// </summary>
        /// <typeparam name="T">將返回資料綁定到指定類型 T</typeparam>
        /// <param name="sql">預存程序名稱</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <param name="output">指定綁定欄位的 SQL 對應型態</param>
        /// <returns>綁定預存程序輸出數值的資料模型</returns>
        public T Exec<T>(string sql, object param = null, object output = null)
        {
            Cmd.CommandText = sql;
            Cmd.CommandType = CommandType.StoredProcedure;
            Cmd.Parameters.Clear();
            setParaInput(Cmd, param);
            Dictionary<string, SqlParameter> tmpOutputParams = output == null ? null : setParaOutput(Cmd, output);
            Cmd.ExecuteNonQuery();
            return getParaOutput<T>(tmpOutputParams);
        }

        /// <summary>
        /// [有風險] 手動開啟連線並執行預存程序，需自行控制可能發生的錯誤
        /// </summary>
        /// <param name="sql">預存程序名稱</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <param name="output">指定綁定欄位的 SQL 對應型態</param>
        /// <returns>綁定預存程序輸出數值的動態資料</returns>
        public dynamic DynamicExec(string sql, object param = null, object output = null)
        {
            Cmd.CommandText = sql;
            Cmd.CommandType = CommandType.StoredProcedure;
            Cmd.Parameters.Clear();
            setParaInput(Cmd, param);
            Dictionary<string, SqlParameter> tmpOutputParams = output == null ? null : setParaOutput(Cmd, output);
            Cmd.ExecuteNonQuery();
            return getDynamicParaOutput(tmpOutputParams);
        }

        /// <summary>
        /// 自動開啟連線並執行查詢語法，執行完畢後自動關閉連線
        /// </summary>
        /// <typeparam name="T">將返回資料綁定到指定類型</typeparam>
        /// <param name="sql">查詢語法</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <param name="isfetchall">是否取出所有資料，預設為 true，否則只取出 1 筆</param>
        /// <returns>綁定查詢語法輸出表格的資料模型陣列</returns>
        public T[] QuickQuery<T>(string sql, object param = null, bool isfetchall = true)
        {
            T[] data = null;
            Connet();
            if (IsConn)
            {
                try
                {
                    SqlDataReader stmt = Query(sql, param);
                    if (stmt != null)
                    {
                        data = fetch<T>(stmt, isfetchall);
                        stmt.Close();
                    }
                    if (isTran) Tran.Commit();
                }
                catch (Exception e)
                {
                    log.Write("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    errormessage.Add("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    log.Write(e.ToString());

                    try
                    {
                        if (isTran)
                            Tran.Rollback();
                    }
                    catch (Exception x)
                    {
                        log.Write("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        errormessage.Add("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        log.Write(x.ToString());
                    }
                }
                Close();
            }
            return data;
        }

        /// <summary>
        /// [有風險] 自動開啟連線並執行查詢語法，執行完畢後自動關閉連線
        /// </summary>
        /// <param name="sql">查詢語法</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <param name="isfetchall">是否取出所有資料，預設為 true，否則只取出 1 筆</param>
        /// <returns>綁定查詢語法輸出表格的動態資料陣列</returns>
        public dynamic[] QuickDynamicQuery(string sql, object param = null, bool isfetchall = true)
        {
            dynamic[] data = null;
            Connet();
            if (IsConn)
            {
                try
                {
                    SqlDataReader stmt = Query(sql, param);
                    if (stmt != null)
                    {
                        data = dynamicFetch(stmt, isfetchall);
                        stmt.Close();
                    }
                    if (isTran) Tran.Commit();
                }
                catch (Exception e)
                {
                    log.Write("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    errormessage.Add("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    log.Write(e.ToString());
                    try
                    {
                        if (isTran)
                            Tran.Rollback();
                    }
                    catch (Exception x)
                    {
                        log.Write("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        errormessage.Add("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        log.Write(x.ToString());
                    }
                }
                Close();
            }
            return data;
        }

        /// <summary>
        /// 自動開啟連線並執行預存程序，執行完畢後自動關閉連線
        /// </summary>
        /// <typeparam name="T">將返回資料綁定到指定類型</typeparam>
        /// <param name="sql">預存程序名稱</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <param name="output">指定綁定欄位的 SQL 對應型態</param>
        /// <returns>綁定預存程序輸出數值的資料模型</returns>
        public T QuickExec<T>(string sql, object param = null, object output = null)
        {
            T obj = default(T);
            Connet();
            if (IsConn)
            {
                try
                {
                    obj = Exec<T>(sql, param, output);
                    if (isTran) Tran.Commit();
                }
                catch (Exception e)
                {
                    log.Write("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    errormessage.Add("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    log.Write(e.ToString());
                    try
                    {
                        if (isTran)
                            Tran.Rollback();
                    }
                    catch (Exception x)
                    {
                        log.Write("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        errormessage.Add("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        log.Write(x.ToString());
                    }
                }
                Close();
            }
            return obj;
        }

        /// <summary>
        /// [有風險] 自動開啟連線並執行預存程序，執行完畢後自動關閉連線
        /// </summary>
        /// <param name="sql">預存程序名稱</param>
        /// <param name="param">語法中的參數化資料</param>
        /// <param name="output">指定綁定欄位的 SQL 對應型態</param>
        /// <returns>綁定預存程序輸出數值的動態資料</returns>
        public dynamic QuickDynamicExec(string sql, object param = null, object output = null)
        {
            dynamic obj = null;
            Connet();
            if (IsConn)
            {
                try
                {
                    obj = DynamicExec(sql, param, output);
                    if (isTran) Tran.Commit();
                }
                catch (Exception e)
                {
                    log.Write("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    errormessage.Add("SQL Error:" + sql + " para:" + JsonConvert.SerializeObject(param));
                    log.Write(e.ToString());
                    try
                    {
                        if (isTran)
                            Tran.Rollback();
                    }
                    catch (Exception x)
                    {
                        log.Write("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        errormessage.Add("SQL Error: Can not rollback, " + sql + " para:" + JsonConvert.SerializeObject(param));
                        log.Write(x.ToString());
                    }
                }
                Close();
            }
            return obj;
        }

        /// <summary>
        /// 自動開啟連線並執行大量資料寫入作業，執行完畢後自動關閉連線
        /// </summary>
        /// <param name="data">儲存大量資料的資料表物件</param>
        /// <param name="tableName">寫入的資料表名稱</param>
        /// <returns>是否寫入成功</returns>
        public bool quickBulkCopy(DataTable data, string tableName)
        {
            bool result = false;
            Connet();
            if (IsConn)
            {
                try
                {
                    using (var bcp = new SqlBulkCopy(Conn, SqlBulkCopyOptions.FireTriggers, null))
                    {
                        bcp.DestinationTableName = tableName;
                        bcp.WriteToServer(data);
                    }
                    if (isTran) Tran.Commit();
                    result = true;
                }
                catch (Exception e)
                {
                    log.Write("SQL BCP Error: " + tableName);
                    errormessage.Add("SQL BCP Error: " + tableName);
                    log.Write(e.ToString());
                    if (isTran) Tran.Rollback();
                }
                Close();
            }
            return result;
        }

        /// <summary>
        /// 手動執行取出查詢結果的資料，並綁定到指定的資料模型中
        /// </summary>
        /// <typeparam name="T">將資料綁定到指定類型</typeparam>
        /// <param name="r">資料讀取物件</param>
        /// <param name="fetchAll">是否取出所有資料，預設為 true，否則只取出 1 筆</param>
        /// <returns>綁定資料的模型陣列</returns>
        public T[] fetch<T>(SqlDataReader r, bool fetchAll = true)
        {
            if (r == null) return null;
            List<T> data = new List<T>();
            T obj = default(T);
            while (r.Read())
            {
                obj = (T)Activator.CreateInstance(typeof(T));
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var value = r.GetValue(i);
                    var prop = obj.GetType().GetProperty(r.GetName(i), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if ((prop != null) && prop.CanWrite)
                        prop.SetValue(obj, Convert.IsDBNull(value) ? null : value, null);
                }
                data.Add(obj);
                if (!fetchAll) break;
            }
            return data.ToArray();
        }

        /// <summary>
        /// 手動執行取出查詢結果的資料，並綁定到動態資料中
        /// </summary>
        /// <param name="r">資料讀取物件</param>
        /// <param name="fetchAll">是否取出所有資料，預設為 true，否則只取出 1 筆</param>
        /// <returns>綁定資料的動態資料陣列</returns>
        public dynamic[] dynamicFetch(SqlDataReader r, bool fetchAll = true)
        {
            if (r == null) return null;
            List<dynamic> data = new List<dynamic>();
            while (r.Read())
            {
                IDictionary<string, object> dict = new ExpandoObject() as IDictionary<string, object>;
                for (int i = 0; i < r.FieldCount; i++)
                {
                    var value = r.GetValue(i);
                    var key = r.GetName(i);
                    dict[key] = Convert.IsDBNull(value) ? null : value;
                }
                data.Add(dict);
                if (!fetchAll) break;
            }
            return data.ToArray();
        }

        /*
            bind sql params values          
        */
        private void setParaInput(SqlCommand cmd, object param)
        {
            if (param != null)
                foreach (var prop in param.GetType().GetProperties())
                {
                    var value = prop.GetValue(param, null) ?? DBNull.Value;
                    if (Cast.IsType<IEnumerable>(value)) expandParams(cmd, value, prop.Name);
                    else if (cmd.CommandText.Contains($"@{prop.Name}"))
                        cmd.Parameters.AddWithValue($"@{prop.Name}", value);
                    else continue;
                }
        }

        /*
            expend array data and assign to params one by one 
        */
        private void expandParams(SqlCommand cmd, object value, string key)
        {
            IEnumerable arr = (IEnumerable)value;
            List<string> expandParamNames = new List<string>();
            int idx = 0;
            foreach (object ele in arr)
            {
                string new_name = $"@{key}{idx}";
                cmd.Parameters.AddWithValue(new_name, ele ?? DBNull.Value);
                expandParamNames.Add(new_name);
                idx++;
            }

            if (idx == 0)
                cmd.Parameters.AddWithValue($"@{key}", DBNull.Value);  
            else
                cmd.CommandText = cmd.CommandText.Replace($"@{key}", string.Join(",", expandParamNames));        
        }


        /*
            set output paras
            return output paras set 
        */
        private Dictionary<string, SqlParameter> setParaOutput(SqlCommand cmd, object output)
        {
            if (output == null) return null;
            Dictionary<string, SqlParameter> tmpOutputParams = new Dictionary<string, SqlParameter>();
            foreach (var kv in Mirror.Members(output))
            {
                string key = Cast.To<string>(kv.Key);
                object val = kv.Value;
                if (string.IsNullOrWhiteSpace(key) || val.GetType() != typeof(SqlDbType)) continue;
                SqlDbType type = (SqlDbType)val;
                SqlParameter outputIdParam = (type == SqlDbType.NVarChar) ? new SqlParameter($"@{key}", type, 4000) : new SqlParameter($"@{key}", type);
                outputIdParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outputIdParam);
                tmpOutputParams.Add(key, outputIdParam);
            }
            return tmpOutputParams;
        }

        /*
            get output paras values 
        */
        private T getParaOutput<T>(Dictionary<string, SqlParameter> tmpOutputParams)
        {
            T obj = (T)Activator.CreateInstance(typeof(T));
            if (tmpOutputParams != null)
            {
                foreach (var item in tmpOutputParams)
                {
                    string name = item.Key;
                    object value = item.Value.Value;
                    var prop = obj.GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if ((prop != null) && prop.CanWrite)
                    {
                        if (value.GetType() == typeof(DBNull)) value = null;
                        try
                        {
                            prop.SetValue(obj, value, null);
                        }
                        catch (Exception e)
                        {
                            log.Write("can not covert type mapping to Model: " + e.ToString());
                            errormessage.Add("can not covert type mapping to Model: " + e.ToString());
                        }
                    }
                }
            }
            return obj;
        }

        /*
            [Beta]
            get output paras values 
        */
        private dynamic getDynamicParaOutput(Dictionary<string, SqlParameter> tmpOutputParams)
        {
            IDictionary<string, object> obj = new ExpandoObject() as IDictionary<string, object>;
            if (tmpOutputParams != null)
            {
                foreach (var item in tmpOutputParams)
                {
                    string name = item.Key;
                    object value = item.Value.Value;
                    obj[name] = value.GetType() == typeof(DBNull) ? null : value;
                }
            }
            return obj;
        }

        /// <summary>
        /// 手動關閉資料庫連線
        /// </summary>
        public void Close()
        {
            if (Conn != null)
                if (Conn.State == ConnectionState.Open)
                    Conn.Close();
        }

    }
}
