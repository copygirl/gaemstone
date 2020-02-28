using gaemstone.Common.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace gaemstone.Common.Utility
{
    public static partial class Logger
    {
        public static void Log(this Universe universe, LogSeverity severity, string format, params object[] Message)
        {
            Console.Write("[");
            switch(severity)
            {
                case LogSeverity.Notice:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
            }

            Console.Write($"{Enum.GetName(typeof(LogSeverity), severity)}");
            Console.ResetColor();
            Console.WriteLine($"]:{string.Format(format, Message)}");
        }


        


    }

    public enum LogSeverity
    {
        Notice,
        Warning,
        Error,
        Critical
    }
}
