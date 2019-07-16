using System;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Collections.Generic;

namespace scrcpyGUI
{
    public static class Sys
    {
        //Constant Field
        public const string programADB = "adb.exe";
        public const string paramADBDevices = "devices -l";

        public const string programPath = "scrcpy.exe";
        public const string paramBitRate = "-b {0}";
        public const string paramCrop = "-c {0}:{1}:{2}:{3}";
        public const string paramFullScreen = "-f";
        public const string paramRecordFmt = "-F";
        public const string paramMaxSize = "-m {0}";
        public const string paramNoControl = "-n";
        public const string paramNoDisplay = "-N";
        public const string paramPort = "-p {0}";
        public const string paramRecord = "-r \"{0}\"";
        public const string paramFlag = "--render-expired-frames";
        public const string paramSerial = "-s {0}";
        public const string paramTurnScreenOff = "-S";
        public const string paramShowTouches = "-t";
        public const string paramAlwaysOnTop = "-T";
        public const string paramVersion = "-v";

        const int VKID_LCTRL = 0xA2;
        const int VKID_RCTRL = 0xA3;
        const int VKID_BACK = 0x08;
        const int VKID_UP = 0x26;
        const int VKID_DOWN = 0x28;

        public enum Command {
            Fullscreen,
            Resize,
            RemBorder,
            ToggleFPS,
            BACK,
            APP_SWITCH,
            MENU,
            VOLUME_UP,
            VOLUME_DOWN
        }
        public class Device {
            public string serial;
            public string productCode;
            public string model;
            public string deviceCode;
            public string transportID;
        }

        //Property Field
        public static StringBuilder output { get { return m_output; } }
        public static StringBuilder error { get { return m_error; } }
        public static bool isRunning {
            get {
                return m_running;
            }
        }
        public static Action onOutputUpdated { get; set; }
        public static Action onErrorUpdated { get; set; }

        //Private Field
        private static StringBuilder m_output;
        private static StringBuilder m_error;

        private static Process m_procInst;
        private static Process m_procADB;

        private static bool m_running;

        private static Thread m_adbThread;
        private static Thread m_srccpyThread;

        private static List<string> m_adbData;

        //Native Method
        [DllImport("User32.dll")]
        static extern IntPtr SetParent(IntPtr childPtr, IntPtr parentPtr);
        [DllImport("User32.dll", EntryPoint = "FindWindowEx")]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("User32.dll")]
        static extern int SendMessage(IntPtr hWnd, int uMsg, int wParam, string lParam);
        [DllImport("User32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        //Public Method
        public static void StopChecker() {
            if (m_procADB != null && !m_procADB.HasExited) {
                m_procADB.Kill();
                m_procADB.Close();
                m_procADB.Dispose();
            }
            if (m_adbThread != null)
                m_adbThread.Abort();
        }

        public static void RunChecker() {
            StopChecker();
            m_adbThread = new Thread(_adbWorker);
            m_adbThread.Start();
        }

        public static void StopProgram() {
            if (m_procInst != null && !m_procADB.HasExited)
            {
                m_procInst.Kill();
                m_procInst.Close();
                m_procInst.Dispose();
            }
            if (m_srccpyThread != null)
                m_srccpyThread.Abort();
        }

        public static void RunProgram(StringBuilder arg) {
            StopProgram();
            m_output = new StringBuilder();
            m_error = new StringBuilder();

            ParameterizedThreadStart threadParam = _srccpyWorker;
            m_srccpyThread = new Thread(threadParam);
            m_srccpyThread.Start(arg);
        }

        public static void SetCommand(Command command) {
            if (m_procInst != null) {
                switch (command) {
                    case Command.VOLUME_UP:

                        break;
                    case Command.VOLUME_DOWN:

                        break;
                    case Command.MENU:

                        break;
                    case Command.BACK:

                        break;
                    case Command.APP_SWITCH:

                        break;
                }
            }
        }

        public static List<Device> getDevices() {
            if (m_adbData != null && m_adbData.Count > 0) {

            }
        }

        //Listener
        private static void OnProcessOutputDataRecieved(object sender, DataReceivedEventArgs e){
            Console.WriteLine("srccpy: " + e.Data);
            m_output.AppendLine(e.Data);
            onOutputUpdated?.Invoke();
        }
        private static void OnProcErrorDataRecieved(object sender, DataReceivedEventArgs e){
            Console.WriteLine("srccpy: " + e.Data);
            m_error.AppendLine(e.Data);
            onErrorUpdated?.Invoke();
            m_running = false;
            StopProgram();
        }
        private static void OnADBOutputDataRecieved(object sender, DataReceivedEventArgs e) {
            //Console.WriteLine("adb Output: " + e.Data);
            m_adbData.Add(e.Data);
        }
        private static void OnADBErrorDataRecieved(object sender, DataReceivedEventArgs e) {
            Console.WriteLine("adb Error: " + e.Data);
        }

        //Worker / Thread
        private static void _adbWorker() {
            m_procADB = new Process();
            
            m_procADB.StartInfo.FileName = programADB;
            m_procADB.StartInfo.Arguments = paramADBDevices;
            m_procADB.StartInfo.UseShellExecute = false;
            m_procADB.StartInfo.RedirectStandardOutput = true;
            m_procADB.StartInfo.RedirectStandardError = true;
            m_procADB.StartInfo.CreateNoWindow = true;
            m_procADB.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            m_procADB.OutputDataReceived += OnADBOutputDataRecieved;
            m_procADB.ErrorDataReceived += OnADBErrorDataRecieved;

            while (true){
                m_procADB.Start();
                m_adbData = new List<string>();
                m_procADB.BeginOutputReadLine();
                m_procADB.BeginErrorReadLine();
                m_procADB.WaitForExit();
                m_procADB.CancelErrorRead();
                m_procADB.CancelOutputRead();
                Thread.Sleep(10);
            }
        }
        
        private static void _srccpyWorker(object arg) {
            m_procInst = new Process();

            m_procInst.StartInfo.FileName = programPath;
            m_procInst.StartInfo.Arguments = ((StringBuilder)arg).ToString();
            m_procInst.StartInfo.UseShellExecute = false;
            m_procInst.StartInfo.RedirectStandardOutput = true;
            m_procInst.StartInfo.RedirectStandardError = true;
            m_procInst.StartInfo.CreateNoWindow = true;
            m_procInst.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            m_procInst.OutputDataReceived += OnProcessOutputDataRecieved;
            m_procInst.ErrorDataReceived += OnProcErrorDataRecieved;

            m_procInst.Start();
            m_procInst.BeginOutputReadLine();
            m_procInst.BeginErrorReadLine();
            m_running = true;
            m_procInst.WaitForExit();
            m_running = false;
        }

        //Klass
    }
}
