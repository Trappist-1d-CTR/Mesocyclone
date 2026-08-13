#if DEV
    #define ENABLE_LOGGING // yeah so idk why you're supposed to define symbols through unity's API rather than just as a preprocessor directive in C#???
#endif

using System;
using System.Diagnostics;
using UnityEngine;

namespace Mesocyclone
{
    /// <summary>
    /// Custom logger for the game which should be used for whatever logs should be stripped out on build.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void Log(System.Object message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// Logs a message to the Unity 
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="context">Object to which the message applies.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void Log(System.Object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an assertion message to the console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogAssertion(System.Object message)
        {
            UnityEngine.Debug.LogAssertion(message);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an assertion message to the console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="context">Object to which the message applies</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogAssertion(System.Object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogAssertion(message, context);
        }

        /// <summary>
        /// Logs a formatted assertion message to the Unity Console.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogAssertionFormat(string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogAssertionFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted assertion message to the Unity Console.
        /// </summary>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogAssertionFormat(UnityEngine.Object context, string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogAssertionFormat(context, format, args);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an error message to the Unity Console.
        /// </summary>
        /// <param name="message">String or Object to object to be converted to string representation for display.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogError(System.Object message)
        {
            UnityEngine.Debug.Log(message);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an error message to the Unity Console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="context">Object to which the message applies.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogError(System.Object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }

        /// <summary>
        /// Logs a formatted error message to the Unity Console.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogErrorFormat(string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted error message to the Unity Console.
        /// </summary>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogErrorFormat(UnityEngine.Object context, string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(context, format, args);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an exception message to the Unity Console.
        /// </summary>
        /// <param name="exception">Runtime Exception.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogException(Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }

        /// <summary>
        /// A variant of Logger.Log that logs an exception message to the Unity Console.
        /// </summary>
        /// <param name="exception">Runtime Exception.</param>
        /// <param name="context">Object to which the message applies.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogException(Exception exception, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogException(exception, context);
        }

        /// <summary>
        /// nothing interesting here.
        /// </summary>
        [Conditional("ENABLE_LOGGING")]
        public static void Joar()
        {
            throw new Joar();
        }

        /// <summary>
        /// Logs a formatted message to the Unity Console.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogFormat(string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted message to the Unity Console.
        /// </summary>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogFormat(UnityEngine.Object context, string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogFormat(context, format, args);
        }

        /// <summary>
        /// Logs a formatted message to the Unity Console.
        /// </summary>
        /// <param name="logType">Type of message e.g: warn or error etc.</param>
        /// <param name="logOptions">Option flags to treat the log message special.</param>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogFormat(LogType logType, LogOption logOptions, UnityEngine.Object context, string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogFormat(logType, logOptions, context, format, args);
        }

        /// <summary>
        /// A variant of Logger.Log that logs a warning message to the Unity Console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogWarning(System.Object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        /// <summary>
        /// A variant of Logger.Log that logs a warning message to the Unity Console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="context">Object to which the message applies.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogWarning(System.Object message, UnityEngine.Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }

        /// <summary>
        /// Logs a formatted warning message to the Unity Console.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogWarningFormat(string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(format, args);
        }

        /// <summary>
        /// Logs a formatted warning message to the Unity Console.
        /// </summary>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">Format arguments.</param>
        [Conditional("ENABLE_LOGGING")]
        public static void LogWarningFormat(UnityEngine.Object context, string format, params System.Object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(context, format, args);
        }
    }
}