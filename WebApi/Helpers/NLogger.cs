#region Using namespaces.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Tracing;
using NLog;
using System.Net.Http;
using System.Text;
using WebApi.ErrorHelper;
#endregion

namespace WebApi.Helpers
{
    /// <summary>
    /// Public class to log Error/info messages to the access log file
    /// </summary>
    public sealed class NLogger : ITraceWriter
    {
        #region Private member variables.
        private static readonly Logger ClassLogger = LogManager.GetCurrentClassLogger();

        private static readonly Lazy<Dictionary<TraceLevel, Action<string>>> LoggingMap = new Lazy<Dictionary<TraceLevel, Action<string>>>(() => new Dictionary<TraceLevel, Action<string>> { { TraceLevel.Info, ClassLogger.Info }, { TraceLevel.Debug, ClassLogger.Debug }, { TraceLevel.Error, ClassLogger.Error }, { TraceLevel.Fatal, ClassLogger.Fatal }, { TraceLevel.Warn, ClassLogger.Warn } });
        #endregion

        #region Private properties.
        /// <summary>
        /// Get property for Logger
        /// </summary>
        private Dictionary<TraceLevel, Action<string>> Logger
        {
            get { return LoggingMap.Value; }
        }
        #endregion

        #region Public member methods.
        /// <summary>
        /// Implementation of TraceWriter to trace the logs.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="category"></param>
        /// <param name="level"></param>
        /// <param name="traceAction"></param>
        public void Trace(HttpRequestMessage request, string category, TraceLevel level, Action<TraceRecord> traceAction)
        {
            if (level != TraceLevel.Off)
            {
                if (traceAction != null && traceAction.Target != null)
                {
                    category = category + Environment.NewLine + "Action Parameters : " + traceAction.Target.ToJSON();
                }
                var record = new TraceRecord(request, category, level);
                if (traceAction != null) traceAction(record);
                Log(record);
            }
        }
        #endregion

        #region Private member methods.
        /// <summary>
        /// Logs info/Error to Log file
        /// </summary>
        /// <param name="record"></param>
        private void Log(TraceRecord record)
        {
            var message = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(record.Message))
                message.Append("").Append(record.Message + Environment.NewLine);

            if (record.Request != null)
            {
                if (record.Request.Method != null)
                    message.Append("Method: " + record.Request.Method + Environment.NewLine);

                if (record.Request.RequestUri != null)
                    message.Append("").Append("URL: " + record.Request.RequestUri + Environment.NewLine);

                if (record.Request.Headers != null && record.Request.Headers.Contains("Token") && record.Request.Headers.GetValues("Token") != null && record.Request.Headers.GetValues("Token").FirstOrDefault() != null)
                    message.Append("").Append("Token: " + record.Request.Headers.GetValues("Token").FirstOrDefault() + Environment.NewLine);
            }

            if (!string.IsNullOrWhiteSpace(record.Category))
                message.Append("").Append(record.Category);

            if (!string.IsNullOrWhiteSpace(record.Operator))
                message.Append(" ").Append(record.Operator).Append(" ").Append(record.Operation);

            if (record.Exception != null && !string.IsNullOrWhiteSpace(record.Exception.GetBaseException().Message))
            {
                var exceptionType = record.Exception.GetType();
                message.Append(Environment.NewLine);
                if (exceptionType == typeof(ApiException))
                {
                    var exception = record.Exception as ApiException;
                    if (exception != null)
                    {
                        message.Append("").Append("Error: " + exception.ErrorDescription + Environment.NewLine);
                        message.Append("").Append("Error Code: " + exception.ErrorCode + Environment.NewLine);
                    }
                }
                else if (exceptionType == typeof(ApiBusinessException))
                {
                    var exception = record.Exception as ApiBusinessException;
                    if (exception != null)
                    {
                        message.Append("").Append("Error: " + exception.ErrorDescription + Environment.NewLine);
                        message.Append("").Append("Error Code: " + exception.ErrorCode + Environment.NewLine);
                    }
                }
                else if (exceptionType == typeof(ApiDataException))
                {
                    var exception = record.Exception as ApiDataException;
                    if (exception != null)
                    {
                        message.Append("").Append("Error: " + exception.ErrorDescription + Environment.NewLine);
                        message.Append("").Append("Error Code: " + exception.ErrorCode + Environment.NewLine);
                    }
                }
                else
                    message.Append("").Append("Error: " + record.Exception.GetBaseException().Message + Environment.NewLine);
            }

            Logger[record.Level](Convert.ToString(message) + Environment.NewLine);
        }
        #endregion
    }
}