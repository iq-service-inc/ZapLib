using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using ZapLib.Model;
using ZapLib.Utility;

namespace ZapLib
{
    /// <summary>
    /// 紀錄事件與日誌工具
    /// </summary>
    public class MyLog
    {
        private static readonly object ForceLogLock = new object();

        /// <summary>
        /// 日誌存放路徑
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// 日誌名稱，預設以 yyyy-mm-dd 進行命名，每天只會產生一個檔案
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 使否啟用安靜模式，預設為 NULL，啟用後本類別將不再寫檔
        /// </summary>
        public string SilentMode { get; set; }

        /// <summary>
        /// 讀取 log 的批次大小，預設 1mb
        /// </summary>
        public int PageSize { get; set; } = 2048;
        //public int PageSize { get; set; } = 50;
        
        /// <summary>
        /// 最後一次 Log 檔案寫入位置
        /// </summary>
        public string LastWritePath { get; set; }

        /// <summary>
        /// 建構子，可指定 log 檔案名稱預設將以今天 yyyyMMdd 形式命名，也可自行指定其他日期，以利讀取
        /// </summary>
        public MyLog() { }

        /// <summary>
        /// 建構子，可指定 log 檔案名稱，預設將以今天 yyyyMMdd 形式命名，也可自行指定其他日期，以利讀取
        /// </summary>
        /// <param name="Name">日誌檔名，預設今天 yyyyMMdd</param>
        public MyLog(string Name)
        {

            this.Name = Name;
        }

        /// <summary>
        /// 將訊息寫入實體日誌檔案,優先採用指定路徑，第二採用 Config 設定中的 MyLog，第三採用 Config 設定中的 Storage
        /// </summary>
        /// <param name="msg">訊息</param>
        public void Write(string msg)
        {
            if (SilentMode != null)
            {
                bool.TryParse(SilentMode, out bool _silentMode);
                if (_silentMode) return;
            }

            var lastpath = GetLastPath();
            if (lastpath == null) return;

            string time = DateTime.Now.ToString("HH:mm:ss");
            string txt = string.Format("[{0}] {1}", time, msg) + Environment.NewLine;

            _write(lastpath, txt);
        }

        /// <summary>
        /// 取得最終的log檔案位置，如果無法取得則回傳 Null
        /// </summary>
        /// <returns>log檔案位置</returns>
        private string GetLastPath()
        {
            string logfile = Path ?? Config.Get("MyLog") ?? Config.Get("Storage");
            if (!Directory.Exists(logfile)) return null;
            string name = Name ?? (DateTime.Now.ToString("yyyyMMdd") + ".txt");
            return string.Format(@"{0}\{1}", logfile, name);
        }

        private void _write(string path, string content)
        {
            try
            {
                File.AppendAllText(path, content);
                LastWritePath = path;
            }
            catch (Exception e)
            {
                if (Config.Get("ForceLog") != null)
                {
                    _forceWrite(path, content, e);
                }
            }
        }

        private void _forceWrite(string originalPath, string content, Exception ex)
        {
            try
            {
                string forcePath = GetForcePath(originalPath);
                string forceContent =
                    "[ForceLog]" + Environment.NewLine +
                    "Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine +
                    "OriginalPath: " + originalPath + Environment.NewLine +
                    "Exception:" + Environment.NewLine +
                    ex.ToString() + Environment.NewLine +
                    Environment.NewLine +
                    "Content:" + Environment.NewLine +
                    content + Environment.NewLine +
                    "------------------------------------------------------------" + Environment.NewLine;

                lock (ForceLogLock)
                {
                    File.AppendAllText(forcePath, forceContent);
                    LastWritePath = forcePath;
                }
            }
            catch
            {
                // ForceLog 是最後防線；失敗後不遞迴、不再產生其他 fallback 檔案。
            }
        }

        private string GetForcePath(string originalPath)
        {
            string dir = System.IO.Path.GetDirectoryName(originalPath);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(originalPath);
            string ext = System.IO.Path.GetExtension(originalPath);
            string forceName = string.Format("{0}.force{1}", fileName, ext);

            return string.IsNullOrEmpty(dir) ? forceName : System.IO.Path.Combine(dir, forceName);
        }

        /// <summary>
        /// 將例外物件記錄到 Windows 事件檢視器中
        /// </summary>
        /// <param name="ex">例外物件</param>
        /// <param name="type">事件類型，預設為 Error</param>
        /// <param name="ProjectName">請勿設定</param>
        /// <param name="CodePath">請勿設定</param>
        /// <param name="LineNumber">請勿設定</param>
        public void Event(Exception ex, EventLogEntryType type = EventLogEntryType.Error,
            [System.Runtime.CompilerServices.CallerMemberName] string ProjectName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string CodePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            _Event(ex.ToString(), type, ProjectName, CodePath, LineNumber);
        }

        /// <summary>
        /// 將指定訊息記錄到 Windows 事件檢視器中
        /// </summary>
        /// <param name="msg">指定訊息</param>
        /// <param name="type">事件類型，預設為 Information</param>
        /// <param name="ProjectName">請勿設定</param>
        /// <param name="CodePath">請勿設定</param>
        /// <param name="LineNumber">請勿設定</param>
        public void Event(string msg, EventLogEntryType type = EventLogEntryType.Information,
                    [System.Runtime.CompilerServices.CallerMemberName] string ProjectName = "",
                    [System.Runtime.CompilerServices.CallerFilePath] string CodePath = "",
                    [System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            _Event(msg, type, ProjectName, CodePath, LineNumber);
        }

        private void _Event(string Message, EventLogEntryType type = EventLogEntryType.Error, string ProjectName = "", string CodePath = "", int LineNumber = 0)
        {
            try
            {
                using (EventLog eventLog = new EventLog("Application"))
                {
                    eventLog.Source = "Application";
                    string AppName = AppDomain.CurrentDomain.FriendlyName;
                    string CallingAssembly = Assembly.GetCallingAssembly().GetName().Name;
                    string CallingAssemblyPath = Assembly.GetCallingAssembly().Location;
                    string ExecAssembly = Assembly.GetExecutingAssembly().FullName;
                    string ExecAssemblyPath = Assembly.GetExecutingAssembly().Location;
                    string[] data = new string[]
                    {
                    $"Process = {AppName}" ,
                    $"CallingAssembly = {CallingAssembly}",
                    $"CallingAssemblyPath = {CallingAssemblyPath}" ,
                    $"ExecAssembly = {ExecAssembly}",
                    $"ExecAssemblyPath = {ExecAssemblyPath}" ,
                    $"CodePath = {CodePath}",
                    $"Method = {ProjectName}",
                    $"Log At Line = {LineNumber}",
                    $"Message = {Message}"
                    };
                    eventLog.WriteEvent(new EventInstance(0, 0, type), data);
                }
            }
            catch { }
        }

        /// <summary>
        /// 使用翻頁的機制，從後往前讀取指定的 log 檔案
        /// </summary>
        /// <param name="page">頁數，預設第 1 頁</param>
        /// <returns>Log 讀取結果資料模型</returns>
        public ModelLog Read(int page = 1)
        {
            // 回傳資料模型
            ModelLog log = new ModelLog();
            log.PageSize = PageSize;

            // 計算目前頁數
            int pageidx = Math.Max(1, page);
            log.Page = page;

            // 計算log 檔案位置
            string lastpath = GetLastPath();
            if (lastpath == null)
            {
                log.ErrMsg = "Can not find log file";
                return log;
            }
            log.Path = lastpath;

            // alphabet.txt contains "abcdefghijklmnopqrstuvwxyz"
            using (FileStream fs = new FileStream(lastpath, FileMode.Open, FileAccess.Read))
            {
                int fileLen = (int)fs.Length;
                int start = Math.Min(fileLen, PageSize * pageidx);
                int end = PageSize * (pageidx - 1);

                if (end > start)
                {
                    log.ErrMsg = "The number of pages exceeds the access range";
                    return log;
                }

                // 計算起始與結束位置
                int startidx = fileLen - start;
                int endidx = fileLen - end;

                //Trace.WriteLine("startidx: " + startidx);
                //Trace.WriteLine("endidx: " + endidx);

                // 從起始位置讀檔直到結束位置
                int cnt = endidx - startidx;
                byte[] data = new byte[cnt];
                fs.Seek(startidx, SeekOrigin.Begin);
                fs.Read(data, 0, cnt);



                double _maxpage = fileLen / PageSize;
                log.Result = true;
                log.Data = Encoding.UTF8.GetString(data);

                try
                {
                    log.MaxPage = Convert.ToInt32(Math.Ceiling(_maxpage));
                }
                catch (Exception e)
                {
                    log.ErrMsg = e.ToString();
                }

                return log;
            }
        }
    }
}
