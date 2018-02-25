using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ContestAutomationWindowsService
{
    public partial class ContestAutomationService : ServiceBase
    {
        System.Timers.Timer _timer;
        DateTime _scheduleTime;
        int count;

        public ContestAutomationService()
        {
            InitializeComponent();
            _timer = new System.Timers.Timer();
            _scheduleTime = DateTime.Today.AddDays(1).AddHours(7); // Schedule to run once a day at 7:00 a.m.
        }
    

        public void WorkProcess(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 1. Process Schedule Task
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo();
            p.StartInfo.CreateNoWindow = true;
            // p.StartInfo.WorkingDirectory =   // I usually set this to the bat file's directory
            p.StartInfo.FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            p.StartInfo.Arguments = string.Format("/C \"{0}\"", "C:\\Projects\\ContestAutomation\\Deploy\\start.bat");
            p.StartInfo.ErrorDialog = false;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;


            // 2. If tick for the first time, reset next run to every 24 hours
            if (_timer.Interval != 24 * 60 * 60 * 1000)
            {
                _timer.Interval = 24 * 60 * 60 * 1000;
            }
        }

        protected override void OnStart(string[] args)
        {
            // For first time, set amount of seconds between current time and schedule time
            _timer.Enabled = true;
            _timer.Interval = _scheduleTime.Subtract(DateTime.Now).TotalSeconds * 1000;
            _timer.Elapsed += new System.Timers.ElapsedEventHandler(WorkProcess);

            LogService("Service is Started");
        }

        protected override void OnStop()
        {
            LogService("Service Stoped");
            _timer.Enabled = false;
        }

        private void LogService(string content)
        {
            FileStream fs = new FileStream(AppDomain.CurrentDomain.BaseDirectory + @"\ContestAutomationLog", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine(content);
            sw.Flush();
            sw.Close();
        }
    }
}
