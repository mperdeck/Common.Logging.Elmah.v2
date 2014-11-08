using System;
using Common.Logging.Configuration;
using Common.Logging.Factory;
using Elmah;
using System.Web;

namespace Common.Logging.Elmah.v2
{
    /// <summary>
    ///     ElmahLoggerFactoryAdapter
    /// </summary>
    public class ElmahLoggerFactoryAdapter : AbstractCachingLoggerFactoryAdapter
    {
        private readonly LogLevel _minLevel = LogLevel.All;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="properties"></param>
        public ElmahLoggerFactoryAdapter(NameValueCollection properties)
            : base(true)
        {
            if (properties != null)
            {
                string value = properties["MinLevel"];
                if (!string.IsNullOrEmpty(value))
                {
                    _minLevel = (LogLevel)Enum.Parse(typeof(LogLevel), value, true);
                }
            }
        }

        /// <summary>
        ///     Get a ILog instance by type name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected override ILog CreateLogger(string name)
        {
            // If there is no context, then HttpContext.Current simply returns null.
            return new ElmahLogger(_minLevel, ErrorLog.GetDefault(HttpContext.Current));
        }
    }
}