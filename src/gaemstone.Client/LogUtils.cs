using gaemstone.Client.Graphics;
using gaemstone.Common.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace gaemstone.Client
{
    public static class LogUtils
    {
        public static LogSeverity ToLogSeverity(this DebugSeverity severity)
        {
            switch(severity)
            {
                case DebugSeverity.Low:
                    return LogSeverity.Warning;
                case DebugSeverity.Medium:
                    return LogSeverity.Error;
                case DebugSeverity.Notification:
                    return LogSeverity.Notice;
                case DebugSeverity.High:
                    return LogSeverity.Critical;
            }
            return LogSeverity.Notice;
        }

    }
}
