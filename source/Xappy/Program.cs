using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GIPC;
using log4net;
using log4net.Config;

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
            XmlConfigurator.Configure();
            using (_trayIcon = new TrayIcon(Resources.runner))
            {
                _trayIcon.AddMenuItem("E&xit", Exit);
                _trayIconAnimator = new TrayIconAnimator(_trayIcon, Resources.runner, Resources.running_0, Resources.running_30, Resources.running_60);
                _marshal = new TestRunnerMarshal(_trayIconAnimator.Busy, _trayIconAnimator.Rest);
                if (!_marshal.Start())
                    Exit();
                else
                {
                    _trayIcon.TipTitle = String.Format("Xappy ({0}:{1})", _marshal.Host, _marshal.Port);
                    _trayIcon.TipText = "Waiting for requests...";
                    _trayIcon.Show();
                    Application.Run();
                }
            }
        }

        private static void Exit()
        {
            var logger = LogManager.GetLogger("Xappy");
            logger.Info("Exit requested...");
            lock (_lock)
            {
                if (_marshal != null)
                {
                    _marshal.Dispose();
                    _marshal = null;
                }
            }
            logger.Info("Exiting now.");
            Application.Exit();
        }
    }
}
