using System;
using System.Net;
using System.Reflection;
using log4net;

namespace HISWEBAPI.Exceptions
{
    public static class LogErrors
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void WriteErrorLog(Exception ex, string location = "")
        {
            if (!log.IsErrorEnabled || ex == null) return;

            try
            {
                IPHostEntry ip = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress[] addr = ip.AddressList;

                string message = $"\n  ***********************************************************************************  \n";
                message += $" Location                : {location}\n";
                message += $" Time Of Error           : {DateTime.Now}\n";
                message += $" Error Message           : {ex.Message}\n";
                // message += $" Error Place             : {ex.StackTrace}\n";
                message += $" Error On Machine        : {Dns.GetHostName()}\n";
                message += $" Error Machine IP Address: {addr.GetValue(0)}\n";
                message += $" Exception Type          : {ex.GetType()}\n";
                message += $" TargetSite              : {ex.TargetSite}\n";

                if (ex.InnerException != null)
                {
                    message += $" Inner Exception Type    : {ex.InnerException.GetType()}\n";
                    message += $" Inner Exception         : {ex.InnerException.Message}\n";
                    message += $" Inner Source            : {ex.InnerException.Source}\n";

                    if (ex.InnerException.InnerException != null)
                    {
                        message += $" Inner Inner Exception   : {ex.InnerException.InnerException.Message}\n";
                        message += $" Inner Inner Source      : {ex.InnerException.InnerException.Source}\n";
                    }

                    if (ex.InnerException.StackTrace != null)
                    {
                        message += $" Inner Stack Trace       : {ex.InnerException.StackTrace}\n";
                    }
                }

                // Write to log4net error appender
                log.Error(message, ex);
            }
            catch
            {
                // Ignore any logging errors
            }
        }
    }
}




