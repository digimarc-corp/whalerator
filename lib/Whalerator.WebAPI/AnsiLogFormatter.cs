/*
   Copyright 2021 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;

namespace Whalerator.WebAPI
{

    /// <summary>
    /// Formats log messages using ANSI color and compact templates
    /// </summary>
    public sealed class AnsiLogFormatter : ConsoleFormatter, IDisposable
    {
        private readonly IDisposable reloadToken;
        private AnsiLogOptions options;        

        /// <summary>
        /// Common ANSI color sequences.
        /// </summary>
        public static class Ansi
        {
            public const string Reset = "\u001b[0m";

            public const string Black = "\u001b[30m";
            public const string Red = "\u001b[31m";
            public const string Green = "\u001b[32m";
            public const string Yellow = "\u001b[33m";
            public const string Blue = "\u001b[34m";
            public const string Magenta = "\u001b[35m";
            public const string Cyan = "\u001b[36m";
            public const string White = "\u001b[37m";
            public const string Gray = "\u001b[90m";
            public const string BrightRed = "\u001b[91m";
            public const string BrightGreen = "\u001b[92m";
            public const string BrightYellow = "\u001b[93m";
            public const string BrightBlue = "\u001b[94m";
            public const string BrightMagenta = "\u001b[95m";
            public const string BrightCyan = "\u001b[96m";
            public const string BrightWhite = "\u001b[97m";
        }

        public AnsiLogFormatter(AnsiLogOptions options) : base(nameof(AnsiLogFormatter)) => this.options = options ?? throw new ArgumentNullException();

        public AnsiLogFormatter(IOptionsMonitor<AnsiLogOptions> optionsMonitor) : base(nameof(AnsiLogFormatter)) =>
            (reloadToken, options) = (optionsMonitor.OnChange(reloadLoggerOptions), optionsMonitor.CurrentValue ?? throw new ArgumentNullException());

        private void reloadLoggerOptions(AnsiLogOptions options) => this.options = options ?? throw new ArgumentNullException();

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

            if (string.IsNullOrEmpty(message) && logEntry.Exception == null)
            {
                return;
            }
            else
            {
                var badge = FormatBadge(logEntry.LogLevel, logEntry.Category);

                textWriter.Write(badge);
                textWriter.WriteLine(options.LogSingleLine ? escapeNewlines(message) : message);

                if (options.LogStack && logEntry.Exception != null)
                {
                    textWriter.Write(badge);
                    textWriter.WriteLine(FormatException(logEntry.Exception));
                }
            }
        }

        private static string TimeStamp => DateTime.UtcNow.ToString("o");

        static string escapeNewlines(string str) => str.Replace(Environment.NewLine, "\\n");

        /// <summary>
        /// Formats exception details, optionally escaping newlines
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public string FormatException(Exception ex)
        {
            var sb = new StringBuilder();
            sb.Append(Ansi.Yellow);
            sb.Append(options.LogSingleLine ? escapeNewlines(ex.ToString()) : ex.ToString());
            sb.Append(Ansi.Reset);

            return sb.ToString();
        }

        /// <summary>
        /// Creates a colored badge based on a LogLevel
        /// </summary>
        /// <param name="level"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public string FormatBadge(LogLevel level, string category)
        {
            var sb = new StringBuilder();

            // set badge color
            sb.Append(ansiColor(level));

            // add badge text
            sb.Append(level switch
            {
                LogLevel.Trace => traceBadge,
                LogLevel.Debug => debugBadge,
                LogLevel.Information => informationBadge,
                LogLevel.Warning => warningBadge,
                LogLevel.Error => errorBadge,
                LogLevel.Critical => criticalBadge,
                _ => $"{level} ? "
            });

            // add optional timestamp
            if (options.LogTimestamp)
            {
                sb.Append(TimeStamp);
                sb.Append(' ');
            }

            // add optional category
            if (options.LogCategory)
            {
                sb.Append(Ansi.Gray);
                sb.Append(category);
                sb.Append(' ');
            }

            // reset color
            sb.Append(Ansi.Reset);

            return sb.ToString();
        }

        static string ansiColor(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => Ansi.Cyan,
            LogLevel.Debug => Ansi.Magenta,
            LogLevel.Information => Ansi.Green,
            LogLevel.Warning => Ansi.Yellow,
            LogLevel.Error => Ansi.Red,
            LogLevel.Critical => Ansi.BrightRed,
            _ => Ansi.Reset
        };

        // build these strings once up front, to save interpolation or concatenation overhead later
        readonly static string traceBadge = $"{(int)LogLevel.Trace} T ";
        readonly static string debugBadge = $"{(int)LogLevel.Debug} D ";
        readonly static string informationBadge = $"{(int)LogLevel.Information} I ";
        readonly static string warningBadge = $"{(int)LogLevel.Warning} W ";
        readonly static string errorBadge = $"{(int)LogLevel.Error} E ";
        readonly static string criticalBadge = $"{(int)LogLevel.Critical} C ";

        #region IDisposable

        private bool disposedValue;

        private void dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    reloadToken?.Dispose();
                    options = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MsfLogFormatter()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
