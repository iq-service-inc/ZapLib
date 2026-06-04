using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZapLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using ZapLib.Utility;
using ZapLib.Model;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ZapLib.Tests
{
    [TestClass()]
    public class MyLogTests
    {
        [TestMethod()]
        public void write()
        {
            MyLog log = new MyLog();
            log.SilentMode = Config.Get("SilentMode");
            log.Path = "D:\\Log";
            log.Write("wewfewfewfewfewf");
            log.Write("safawfqafw");
            Assert.IsNotNull(log);
        }

        [TestMethod()]
        public void write2()
        {
            MyLog log = new MyLog();
            log.SilentMode = Config.Get("SilentMode");
            log.Path = "D:\\Log";
            log.Name = "MyProcessLogFile";

            log.Write("程式開始...");
            Thread.Sleep(3000);
            log.Write("程式結束...");

            Assert.IsNotNull(log);
        }


        [TestMethod()]
        public void forcewrite()
        {
            FileInfo file = new FileInfo(@"D:\Storage\20190321.txt");
            var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            MyLog log = new MyLog();
            log.Name = "20190321";
            log.Write("程式開始...");
            log.Write("程式結束...");
            if (stream != null) stream.Close();
            Assert.IsNotNull(log);
        }

        [TestMethod()]
        public void write1()
        {
            try
            {
                int x = 0;
                int i = 100 / x;
                Console.WriteLine(i);
            }
            catch (Exception e)
            {
                MyLog log = new MyLog();
                log.SilentMode = Config.Get("SilentMode");
                log.Write(e.ToString());
            }
        }

        [TestMethod()]
        public void WriteTest()
        {
            MyLog log = new MyLog();
            log.Path = "D:\\QQQ";
            log.Write("123");
        }

        [TestMethod()]
        public void ReadTest()
        {
            //string LogFileName = new DateTime(2021,12,23).ToString("yyyyMMdd");

            string LogFileName = new DateTime(2021, 12, 21).ToString("yyyyMMdd");

            MyLog log = new MyLog(LogFileName);
            // 改變每一頁的內容大小 (byte)
            log.PageSize = 2048;
            // 取得第一頁
            ModelLog logres = log.Read(1);

            if (logres.Result)
            {
                Trace.WriteLine("Success");
                Trace.WriteLine(JsonConvert.SerializeObject(logres));
            }



            // 取得第二頁
            logres = log.Read(2);
            if (logres.Result)
            {
                Trace.WriteLine("Success");
                Trace.WriteLine(JsonConvert.SerializeObject(logres));
            }
            else
            {
                Trace.WriteLine("Fail");
                Trace.WriteLine(JsonConvert.SerializeObject(logres));
            }



            Assert.IsTrue(logres.Result);



        }

        [TestMethod()]
        public void WriteTest1()
        {
            MyLog log = new MyLog("mylog.log");
            log.Write("這是一個測試LOG");
            Trace.WriteLine(log.LastWritePath);
            Assert.IsTrue(File.Exists(log.LastWritePath));

        }

        [TestMethod()]
        public void ForceLogWritesSingleFallbackFileWhenOriginalFileLocked()
        {
            string originalForceLog = Config.Get("ForceLog");
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ZapLibForceLog_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                Config.SetOrAdd("ForceLog", "true");
                string name = "system-error.log";
                string originalPath = System.IO.Path.Combine(dir, name);
                string forcePath = System.IO.Path.Combine(dir, "system-error.force.log");
                File.WriteAllText(originalPath, "locked");

                using (FileStream stream = new FileStream(originalPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    MyLog log = new MyLog(name);
                    log.Path = dir;
                    log.Write("first force message");
                    log.Write("second force message");

                    Assert.AreEqual(forcePath, log.LastWritePath);
                    Assert.IsTrue(File.Exists(forcePath));

                    string forceContent = File.ReadAllText(forcePath);
                    StringAssert.Contains(forceContent, "[ForceLog]");
                    StringAssert.Contains(forceContent, "OriginalPath: " + originalPath);
                    StringAssert.Contains(forceContent, "Exception:");
                    StringAssert.Contains(forceContent, "first force message");
                    StringAssert.Contains(forceContent, "second force message");
                    Assert.AreEqual(0, Directory.GetFiles(dir, "system-error.log-*").Length);
                }
            }
            finally
            {
                RestoreForceLogConfig(originalForceLog);
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        [TestMethod()]
        public void ForceLogDisabledDoesNotCreateFallbackFile()
        {
            string originalForceLog = Config.Get("ForceLog");
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ZapLibForceLog_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            try
            {
                Config.Delete("ForceLog");
                string name = "system-error.log";
                string originalPath = System.IO.Path.Combine(dir, name);
                string forcePath = System.IO.Path.Combine(dir, "system-error.force.log");
                File.WriteAllText(originalPath, "locked");

                using (FileStream stream = new FileStream(originalPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    MyLog log = new MyLog(name);
                    log.Path = dir;
                    log.Write("no force message");

                    Assert.IsNull(log.LastWritePath);
                    Assert.IsFalse(File.Exists(forcePath));
                    Assert.AreEqual(0, Directory.GetFiles(dir, "system-error.log-*").Length);
                }
            }
            finally
            {
                RestoreForceLogConfig(originalForceLog);
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        private void RestoreForceLogConfig(string originalForceLog)
        {
            if (originalForceLog == null) Config.Delete("ForceLog");
            else Config.SetOrAdd("ForceLog", originalForceLog);
        }
    }
}
