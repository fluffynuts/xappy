using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace Xappy
{
    public abstract class LogCapableObject
    {
        private ILog _logger;
        private ILog Logger
        {
            get
            {
                lock (this)
                {
                    if (_logger == null)
                        _logger = GetLoggerForThisType();
                }
                return _logger;
            }
        }
        protected Action<string> LogDebug { get; private set; }
        protected Action<string> LogInfo { get; private set; }
        protected Action<string> LogWarning { get; private set; }
        protected Action<string> LogError { get; private set; }
        protected Action<string> LogFatal { get; private set; }

        protected LogCapableObject()
        {
            LogDebug = Logger.Debug;
            LogInfo = Logger.Info;
            LogWarning = Logger.Warn;
            LogError = Logger.Error;
            LogFatal = Logger.Fatal;
        }

        private ILog GetLoggerForThisType()
        {
            return LogManager.GetLogger(this.GetType().Name);
        }
    }
}
