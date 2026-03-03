using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Numb.Properties;

namespace Numb
{
    internal static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new NumbApplicationContext());
        }
    }

    public class NumbApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly BackgroundWorker _worker;

        private List<string> _headers;
        private bool _isMuted;

        private const string _processName = "Spotify";
        private const string _configFileName = "Numb.cfg";

        public NumbApplicationContext()
        {
            LoadConfig();
            _trayIcon = new NotifyIcon()
            {
                Icon = Resources.IdleIcon,
                ContextMenu = new ContextMenu(new [] {
                    new MenuItem("Add", OnAddClick),
                    new MenuItem("Exit", OnExitClick),
                }),
                Visible = true
            };
            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += Main;
            _worker.RunWorkerAsync();
        }

        private void LoadConfig()
        {
            if (File.Exists(_configFileName))
            {
                _headers = File.ReadAllLines(_configFileName).ToList();
            }
            if (_headers == null || _headers.Count == 0)
            {
                _headers = new List<string> { "Spotify", "Pop Extra", "Advertisement" };
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            File.WriteAllLines(_configFileName, _headers.ToArray());
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            var process = FindTargetProcess();
            if (process == null)
                return;
            var title = process.MainWindowTitle;
            if (_headers.Contains(title))
                return;
            _headers.Add(title);
            SaveConfig();
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _worker.CancelAsync();
            var process = FindTargetProcess();
            if (process != null)
                SetMuted(process.Id, false);
            Application.Exit();
        }

        private static Process FindTargetProcess()
        {
            var processes = Process.GetProcessesByName(_processName);
            return processes.FirstOrDefault(x => !string.IsNullOrEmpty(x.MainWindowTitle));
        }

        private void Main(object sender, DoWorkEventArgs e)
        {
            Process process = null;
            while (true)
            {
                if (_worker.CancellationPending)
                    return;

                process?.Refresh();
                if (process != null && process.HasExited)
                    process = null;

                if (process == null)
                {
                    process = FindTargetProcess();
                    if (process == null)
                    {
                        _trayIcon.Icon = Resources.IdleIcon;
                        Thread.Sleep(10000);
                        continue;
                    }
                    SetMuted(process.Id, false);
                }

                var shouldMute = _headers.Contains(process.MainWindowTitle);
                if (shouldMute != _isMuted)
                    SetMuted(process.Id, shouldMute);

                Thread.Sleep(1000);
            }
        }

        private void SetMuted(int pid, bool muted)
        {
            _isMuted = muted;
            MixerController.SetApplicationMute(pid, muted);
            _trayIcon.Icon = muted ? Resources.BlockIcon : Resources.StandByIcon;
        }
    }
}
