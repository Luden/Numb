using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Numb.Properties;

namespace Numb
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }
    }
    public class MyCustomApplicationContext : ApplicationContext
    {
        const string TargetName = "Spotify";
        string[] TargetMuteHeaderNames = new string[]{ "Spotify", "Pop Extra" };

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        private NotifyIcon trayIcon;
        private BackgroundWorker _worker;
        bool _isMuted;
        bool _doNotMute;

        public MyCustomApplicationContext()
        {
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.IdleIcon,
                ContextMenu = new ContextMenu(new MenuItem[] {
                new MenuItem("Exit", Exit)
            }),
                Visible = true
            };

            trayIcon.Click += TrayIcon_Click;

            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Main;
            _worker.RunWorkerAsync();
        }

        private void TrayIcon_Click(object sender, EventArgs e)
        {
            _doNotMute = false;
        }

        void Exit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            _worker.CancelAsync();
            Application.Exit();
        }

        void Main(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            Process process = null;
            while (true)
            {
                if (_worker.CancellationPending)
                    return;

                if (process != null)
                    process.Refresh();

                if (process != null && process.HasExited)
                    process = null;

                if (process == null)
                {
                    var processes = Process.GetProcessesByName(TargetName);
                    process = processes.FirstOrDefault(x => !string.IsNullOrEmpty(x.MainWindowTitle));
                    if (process == null)
                    {
                        trayIcon.Icon = Resources.IdleIcon;
                        Thread.Sleep(10000);
                        continue;
                    }
                    SetMuted(process.Id, false, true);
                }

                if (GetForegroundWindow() == process.MainWindowHandle)
                    _doNotMute = true;

                if (TargetMuteHeaderNames.Contains(process.MainWindowTitle))
                {
                    SetMuted(process.Id, !_doNotMute);
                }
                else
                {
                    if (_doNotMute)
                        _doNotMute = false;
                    SetMuted(process.Id, false);
                }

                Thread.Sleep(1000);
            }
        }

        void SetMuted(int pid, bool muted, bool force = false)
        {
            if (muted == _isMuted && !force)
                return;

            _isMuted = muted;
            MixerController.SetApplicationMute(pid, muted);
            trayIcon.Icon = muted ? Resources.BlockIcon : Resources.StandByIcon;
        }
    }
}
