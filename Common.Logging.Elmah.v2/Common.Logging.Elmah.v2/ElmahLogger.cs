using System;
using Common.Logging.Factory;
using System.Web;
using Elmah;
using System.Reflection;

namespace Common.Logging.Elmah.v2
{
    /// <summary>
    ///     ElmahLogger
    /// </summary>
    public class ElmahLogger : AbstractLogger
    {
        private readonly ErrorLog _errorLog;
        private readonly LogLevel _minLevel;

        /// <summary>
        ///     ElmahLogger
        /// </summary>
        public ElmahLogger(LogLevel minLevel, ErrorLog errorLog)
        {
            _minLevel = minLevel;
            _errorLog = errorLog;
        }

        #region ILog Members

        /// <summary>
        ///     Gets a value indicating whether this instance is trace enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is trace enabled; otherwise, <c>false</c>.
        /// </value>
        public override bool IsTraceEnabled
        {
            get { return true; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is debug enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is debug enabled; otherwise, <c>false</c>.
        /// </value>
        public override bool IsDebugEnabled
        {
            get { return true; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is info enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is info enabled; otherwise, <c>false</c>.
        /// </value>
        public override bool IsInfoEnabled
        {
            get { return true; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is warn enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is warn enabled; otherwise, <c>false</c>.
        /// </value>
        public override bool IsWarnEnabled
        {
            get { return true; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is error enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is error enabled; otherwise, <c>false</c>.
        /// </value>
        public override bool IsErrorEnabled
        {
            get { return true; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is fatal enabled.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is fatal enabled; otherwise, <c>false</c>.
        /// </value>
        public override bool IsFatalEnabled
        {
            get { return true; }
        }

        #endregion ILog Members

        /// <summary>
        ///     Actually sends the message to the underlying log system.
        /// </summary>
        /// <param name="logLevel">the level of this log event.</param>
        /// <param name="message">the message to log</param>
        /// <param name="exception">the exception to log (may be null)</param>
        protected override void WriteInternal(LogLevel logLevel, object message, Exception exception)
        {
            if (logLevel < _minLevel) 
                return;

            var context = HttpContext.Current;

            Error error = null;
            string _message = message == null ? null : message.ToString();

            try
            {
                error = exception == null ? new Error(new Exception(_message), context) : new Error(exception, context);
            }
            catch
            {
            }

            if (error == null || context == null)
            {
                error = exception == null ? new Error(new Exception()) : new Error(exception);
            }

            error.Message = _message;
            error.Type = error.Type ?? logLevel.ToString();

            if (exception == null && logLevel < LogLevel.Error)
            {
                //remove all exception details
                error.Type = logLevel.ToString();
                error.Detail = string.Empty; //_message;
                error.Source = string.Empty;

                try
                {
                    //try remove the dummy exception itself using reflection
                    var _exceptionField = error.GetType().GetField("_exception", BindingFlags.GetField 
                        | BindingFlags.NonPublic | BindingFlags.Public);

                    _exceptionField.SetValue(error, null);
                }
                catch
                {

                }
            }

            string appName = _errorLog.ApplicationName;
            bool appNameNotFromErrorLog = false;

            if (string.IsNullOrWhiteSpace(appName) || appName == "/")
            {
                //something went wrong in error log. it's a bug.
                //try this
                appName = HttpRuntime.AppDomainAppId;
                appNameNotFromErrorLog = true;
            }

            if (string.IsNullOrWhiteSpace(appName) || appName == "/")
            {
                //something went wrong in httpruntime appdomainappid
                //try this

                try
                {
                    //usually, Asp.Net would separate the app name from the domain id with a -
                    appName = HttpRuntime.AppDomainId.Substring(0, HttpRuntime.AppDomainId.IndexOf("-"));
                    appNameNotFromErrorLog = true;
                }
                catch
                {

                }
            }

            if (string.IsNullOrWhiteSpace(appName) || appName == "/")
            {
                appName = string.Empty;
            }

            if (appNameNotFromErrorLog && !string.IsNullOrEmpty(appName))
            {
                try
                {
                    _errorLog.ApplicationName = appName;
                }
                catch
                {
                    //try to use reflection
                    try
                    {
                        var _appNameField = _errorLog.GetType().GetField("_appName", BindingFlags.GetField
                            | BindingFlags.NonPublic | BindingFlags.Public);

                        _appNameField.SetValue(_errorLog, appName);
                    }
                    catch
                    {

                    }
                }
            }

            error.ApplicationName = appName; //always do this, without which, error would only log to db and not show under Elmah.axd

            try
            {
                _errorLog.Log(error);
            }
            catch (Exception ex)
            {
                try
                {
                    var messageException = new Exception((string)message, exception);
                    ErrorSignal.FromCurrentContext().Raise(messageException, context);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.Print("Error: {0}\n{1}\n{2}\n{3}", ex.Message, ex.StackTrace, ex2.Message, ex2.StackTrace);
                }
            }
        }
    }
}

