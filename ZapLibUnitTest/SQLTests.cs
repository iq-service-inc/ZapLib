using ZapLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Newtonsoft.Json;
using ZapLib.Utility;

namespace ZapLib.Tests
{
    [TestClass()]
    public class SQLTests
    {
        [TestMethod()]
        public void getErrorMessage()
        {
            /*
              public int Timeout { get; set; } = 15;
        public bool Encrypt { get; set; } = false;
        public string ApplicationIntent { get; set; } = "ReadWrite";
        public bool MultiSubnetFailover { get; set; } = false;
        public bool TrustServerCertificate { get; set; } = false;
             */
            string Host = "10.190.173.190";
            string DBName = "Fpage";
            string User = "sa";
            string Password = "1qaz@WSX";

            SQL db = new SQL(Host, DBName, User, Password);
            db.Timeout = 15;
            db.Encrypt = false;
            db.ApplicationIntent = "ReadWrite";
            db.MultiSubnetFailover = false;
            db.TrustServerCertificate = false;


            object[] o = db.QuickQuery<object>("select * from object where oid>@oid", new { oid = 1 });


            string error = db.GetErrorMessage();
            Trace.WriteLine("------------------------");
            Trace.WriteLine(error);
            Trace.WriteLine("------------------------");
            Assert.IsNotNull(error);
        }


        [TestMethod()]
        public void quickExec()
        {
            string Host = "10.190.173.190";
            string DBName = "Fpage";
            string User = "sa";
            string Password = "1qaz@WSX";

            SQL db = new SQL(Host, DBName, User, Password);

            var input_para = new
            {
                act = "admin",
                passportcode = "123456789"
            };

            var output_para = new
            {
                res = SqlDbType.Int
            };

            ModelOutput result = db.QuickExec<ModelOutput>("xp_checklogin", input_para);

            if (result == null)
                Console.WriteLine(db.GetErrorMessage());
            else
                Console.WriteLine(db.GetErrorMessage());
        }

        [TestMethod()]
        public void detailControl()
        {
            string Host1 = "192.168.1.190";
            string DBName1 = "TestFpage";
            string User1 = "sa";
            string Password1 = "1qaz@WSX";

            string Host2 = "192.168.1.190";
            string DBName2 = "TestFpage";
            string User2 = "sa";
            string Password2 = "1qaz@WSX";

            SQL db1 = new SQL(Host1, DBName1, User1, Password1);
            SQL db2 = new SQL(Host2, DBName2, User2, Password2);

            db1.Connect();
            db2.Connect();

            if (db1.IsConn && db2.IsConn)
            {
                string sql_1 = "select oid from object";
                string sql_2 = "insert into TestTable(oid) values(@oid)";

                SqlDataReader reader1 = db1.Query(sql_1);
                ModelObject[] data1 = db1.fetch<ModelObject>(reader1, false);
                while (data1 != null && data1.Length > 0)
                {
                    int id = data1[0].oid;
                    var para = new
                    {
                        oid = id
                    };
                    SqlDataReader reader2 = db2.Query(sql_2, para);
                    var data2 = db2.fetch<ModelObject>(reader2, false);
                    if (data2 == null)
                        Console.WriteLine("inserted fail");
                    reader2.Close();
                    data1 = db1.fetch<ModelObject>(reader1, false);
                }
                reader1.Close();
            }
            db1.Close();
            db2.Close();

        }

        [TestMethod()]
        public void bulkCopy()
        {

            var dt = new DataTable();
            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("oid", typeof(int));

            for (int i = 0; i < 100; i++)
            {
                var row = dt.NewRow();
                row["oid"] = i;
                row["name"] = "user_" + i;
                dt.Rows.Add(row);
            }
            string Host = "10.190.173.190";
            string DBName = "TestFpage";
            string User = "sa";
            string Password = "1qaz@WSX";
            SQL db = new SQL(Host, DBName, User, Password);

            bool result = db.QuickBulkCopy(dt, "dbo.TestTable");

            if (!result)
                Console.WriteLine(db.GetErrorMessage());

        }

        [TestMethod()]
        public void transaction()
        {
            string Host = "10.9.173.190";
            string DBName = "TestFpage";
            string User = "sa";
            string Password = "1qaz@WSX";


            string sql_1 = "insert into TestTable(oid) values(@oid)";
            string sql_2 = "insert2 into TestTable(oid) values(@oid)";

            SQL db = new SQL(Host, DBName, User, Password, true);

            db.Connect();

            if (db.IsConn)
            {
                try
                {
                    var reader = db.Query(sql_1, new { oid = 10 });
                    reader.Close();
                    reader = db.Query(sql_2, new { oid = 20 });
                    reader.Close();
                    db.Tran.Commit();
                }
                catch (Exception e)
                {
                    db.Tran.Rollback();
                    Trace.WriteLine(e.ToString());
                }
            }
            db.Close();
        }

        [TestMethod()]
        public void quickDynamicQuery()
        {
            string Host = "192.168.1.190";
            string DBName = "TestFpage";
            string User = "sa";
            string Password = "1qaz@WSX";
            SQL db = new SQL(Host, DBName, User, Password);
            dynamic[] data = db.QuickDynamicQuery("select * from entity");

            for (int i = 0; i < data.Length; i++)
            {
                Trace.WriteLine((string)data[i].cname);
            }
            Assert.IsNotNull(data);
        }
        [TestMethod()]
        public void quickDynamicQuery2()
        {
            string Host = "192.168.1.190";
            string DBName = "TestFpage";
            string User = "sa";
            string Password = "1qaz@WSX";
            SQL db = new SQL(Host, DBName, User, Password);
            dynamic[] data = db.QuickDynamicQuery("select * from entity");

            for (int i = 0; i < data.Length; i++)
            {
                Trace.WriteLine((string)data[i].cname);
            }
            Assert.IsNotNull(data);
        }




        [TestMethod()]
        public void QuickQueryTest()
        {
            SQL db = new SQL("DriveConnectionString");
            ModelNullAbleEnumObject[] res = db.QuickQuery<ModelNullAbleEnumObject>("select top 1 oid , uuid from object");
            Trace.WriteLine(JsonConvert.SerializeObject(res));
            if (res == null) db.GetErrorMessage();
            Assert.IsNotNull(res);
        }

        [TestMethod()]
        public void BuildConnectionStringTest()
        {
            SQL db = new SQL();

            string[] testpool = new string[] {
                @"Data Source=10.190.173.134\support6;Min Pool Size=0;Max Pool Size=100;Pooling=true;Initial Catalog=Drive;Persist Security Info=True;User ID=sa;Password=123456",
                @"Data Source=10.190.173.134\support6;Min Pool Size=0;Max Pool Size=100;Pooling=true;Initial Catalog=Drive;Persist Security Info=True;User ID=sa;Password=123456;Connect Timeout=999",
            };

            string[] exps = new string[]{
                ";Connect Timeout=30",
                ";Connect Timeout=999",
            };

            for (int i = 0; i < testpool.Length; i++)
            {
                string cs = testpool[i];
                string res = new SQL(cs).BuildConnectionString();
                Trace.WriteLine(cs);
                Trace.WriteLine(res);
                Assert.IsTrue(res.Contains(exps[i]));
            }


        }



        [TestMethod()]
        public void BuildConnectionStringTest2()
        {

            string testsql = "Data Source=10.190.173.134\\support6;Initial Catalog=Drive;User ID=sa;Password=123";
            var db = new SQL(testsql);
            string connstr = db.BuildConnectionString();
            Trace.WriteLine(testsql + "\n" + connstr);
            Assert.IsTrue(connstr.Contains("ReadWrite"));

            db.SQLReadOnly = true;

            string connstr2 = db.BuildConnectionString();
            Trace.WriteLine(testsql + "\n" + connstr2);

            Assert.IsTrue(connstr2.Contains("ReadWrite"));

        }



        [TestMethod()]
        public void BuildConnectionStringTest3()
        {
            //SQL db = new SQL();

            string[] testpool = new string[] {
                @"Data Source=10.190.173.134\support6;Min Pool Size=0;Max Pool Size=100;Pooling=true;Initial Catalog=Drive;Persist Security Info=True;User ID=sa;Password=123456",
                @"Data Source=10.190.173.134\support6;Min Pool Size=0;Max Pool Size=100;Pooling=true;Initial Catalog=Drive;Persist Security Info=True;User ID=sa;Password=123456;Connect Timeout=999",
            };


            Trace.WriteLine("case 1");
            string cs = testpool[0];
            var db = new SQL(cs);
            string res = db.BuildConnectionString();
            Trace.WriteLine("conn str: " + cs);
            Trace.WriteLine("build str: " + res);
            Trace.WriteLine("timeout: " + db.Timeout);
            Assert.AreEqual(30, db.Timeout);

            Trace.WriteLine("case 2");
            cs = testpool[1];
            db = new SQL(cs);
            res = db.BuildConnectionString();
            Trace.WriteLine("conn str: " + cs);
            Trace.WriteLine("build str: " + res);
            Trace.WriteLine("timeout: " + db.Timeout);
            Assert.AreEqual(999, db.Timeout);



            Trace.WriteLine("case 3");
            cs = testpool[0];
            db = new SQL(cs);
            db.Timeout = 666;
            res = db.BuildConnectionString();
            Trace.WriteLine("conn str: " + cs);
            Trace.WriteLine("build str: " + res);
            Trace.WriteLine("timeout: " + db.Timeout);
            Assert.AreEqual(666, db.Timeout);


            Trace.WriteLine("case 4");
            cs = testpool[1];
            db = new SQL(cs);
            db.Timeout = 222;
            res = db.BuildConnectionString();
            Trace.WriteLine("conn str: " + cs);
            Trace.WriteLine("build str: " + res);
            Trace.WriteLine("timeout: " + db.Timeout);
            Assert.AreEqual(222, db.Timeout);


        }

        [TestMethod()]
        public void QuickExecuteNoWaitDoesNotOpenCurrentInstanceConnection()
        {
            SQL db = new SQL("Data Source=invalid-server;Initial Catalog=invalid-db;Integrated Security=True;Connect Timeout=1");
            db.Timeout = 1;

            Stopwatch sw = Stopwatch.StartNew();
            db.QuickExecuteNoWait("SELECT @Value", new { Value = 1 });
            sw.Stop();

            Assert.IsNull(db.GetConnection());
            Assert.IsTrue(sw.ElapsedMilliseconds < 500, "QuickExecuteNoWait should return immediately.");
        }

        [TestMethod()]
        public void QuickExecNoWaitDoesNotOpenCurrentInstanceConnection()
        {
            SQL db = new SQL("Data Source=invalid-server;Initial Catalog=invalid-db;Integrated Security=True;Connect Timeout=1");
            db.Timeout = 1;

            Stopwatch sw = Stopwatch.StartNew();
            db.QuickExecNoWait("sp_LongRunningJob", new { JobId = 123 });
            sw.Stop();

            Assert.IsNull(db.GetConnection());
            Assert.IsTrue(sw.ElapsedMilliseconds < 500, "QuickExecNoWait should return immediately.");
        }


        [TestMethod()]
        public void SQLDBReplaceTest()
        {
            (bool, string)[] testcase = new (bool, string)[] {
            (false, "Update KBH522.dbo.Aud_Answer Set fa_sid"),
            (false, "Update [AKBH52].dbo.Aud_Answer Set fa_sid"),
            (false,"Update AKBH52.dbo.Aud_Answer Set fa_sid"),
            (false, @"select* from KbH522.[DbO].Aud_Answer,
            AKbH52.[DbO].Aud_Answer"),
            (true,"Update [KBH52].dbo.Aud_Answer Set fa_sid"),
            (true,"Update kbh52.[dbo].Aud_Answer Set fa_sid"),
            (true,"Update KbH52.DBO.Aud_Answer Set fa_sid"),
            (true,"Update [KbH52   ].[DbO].Aud_Answer Set fa_sid"),
            (true,"select * from [KbH52].[DbO].Aud_Answer c,[KbH52].[DbO].Aud_Answer d"),
            (true,"select* from KbH52.[DbO].Aud_Answer,[KbH52].[DbO].Aud_Answer"),
            (true,  @"select* from KbH52.[DbO].Aud_Answer, 
             [KbH52].[DbO].Aud_Answer"),
            (true, @"select* from KbH52.[DbO].Aud_Answer,
            [bH522].[DbO].Aud_Answer"),
            (true, @"select* from KbH522.[DbO].Aud_Answer,
            [KbH52].[DbO].Aud_Answer"),
            (true, "Update KBH52 . dbo.Aud_Answer Set fa_sid"),
            (true,"Update[KBH52].dbo.Aud_Answer Set fa_sid"),
            (true,"Update kbh52.  [dbo].Aud_Answer Set fa_sid"),
            (true,"Update kbh52.	[dbo].Aud_Answer Set fa_sid"),
            (true,@"Update kbh52.


            [dbo].Aud_Answer Set fa_sid")

           };


            SQL db = new SQL();
            foreach ((bool ans, string q) in testcase)
            {
                string nq = db.SQLDBReplace(q);
                Trace.WriteLine(q);
                Trace.WriteLine(nq);
                bool isdiff = nq != q;
                if (isdiff != ans)
                {
                    Trace.WriteLine(JsonConvert.SerializeObject(db.SQLDBReplaceRules));
                }
                Assert.AreEqual(ans, isdiff);
                Trace.WriteLine("");
            }
        }

        [TestMethod()]
        public void QuickQueryTest1()
        {
            SQL db = new SQL("CSWebAgRW");
            string SqlString = $@"insert into Log_ParamAction(action_id,action_name)values(22,'測試')";
            object[] res = db.QuickQuery<object>(SqlString);
            Assert.IsNotNull(res);
            Trace.WriteLine("寫入:" + res != null);


            SQL db2 = new SQL("CSWebAgRW");
            db2.SQLReadOnly = true;
            string SqlString2 = $@"select * from Log_ParamAction where action_id=22";
            ModelLogSI[] res2 = db2.QuickQuery<ModelLogSI>(SqlString2);
            Assert.IsNotNull(res2);
            Trace.WriteLine(res2[0].action_id + " " + res2[0].action_name);

        }
    }

    enum MyID
    {
        Ok = 0,
        No = 1
    }

    class ModelNullAbleEnumObject
    {
        public MyID? oid { get; set; }
        public string uuid { get; set; }
    }

    class ModelObject
    {
        public int oid { get; set; }
    };

    class ModelOutput
    {
        public int res { get; set; }
    };

    public class ModelBook
    {
        public int id { get; set; }
        public string name { get; set; }
        public DateTime since { get; set; }
    };

    public class ModelAgServer
    {
        /// <summary>連線伺服器名稱</summary>
        public string ServerName { get; set; }

        // <summary>機器名稱</summary>
        public string MachineName { get; set; }
    }


    public class ModelLogSI
    {
        /// <summary>連線伺服器名稱</summary>
        public int param_sid { get; set; }

        // <summary>機器名稱</summary>
        public int action_id { get; set; }
        public string action_name { get; set; }
    }

}
