using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace NetworkedPlanet.Quince
{
    /// <summary>
    /// Static class for configuring the loggers created by Quince
    /// </summary>
    public static class QuinceLogging
    {
        /// <summary>
        /// Retrieve the factory Quince uses to initialise its loggers
        /// </summary>
        public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory();

        internal static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
    }
}
