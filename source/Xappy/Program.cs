using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GIPC;

namespace Xappy
{
    static class Program
    {
        static TestRunnerMarshal _marshal;
        static object _lock = new object();
        private static TrayIcon _trayIcon;
        private static TrayIconAnimator _trayIconAnimator;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (_trayIcon = new TrayIcon(Resources.runner))
            {
                _trayIcon.AddMenuItem("E&xit", Exit);
                _trayIconAnimator = new TrayIconAnimator(_trayIcon, Resources.runner, Resources.running_0, Resources.running_30, Resources.running_60);
                _marshal = new TestRunnerMarshal(_trayIconAnimator.Busy, _trayIconAnimator.Rest);
                if (!_marshal.Start())
                    Exit();
                _trayIcon.Text = String.Format("Xappy: waiting for requests ({0}:{1})...", _marshal.Host, _marshal.Port);
                _trayIcon.Show();
                Application.Run();
            }
        }

        private static void Exit()
        {
            lock (_lock)
            {
                if (_marshal != null)
                {
                    _marshal.Dispose();
                    _marshal = null;
                }
            }
            Application.Exit();
        }
    }
}
