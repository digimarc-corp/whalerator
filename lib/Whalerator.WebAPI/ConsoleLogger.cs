/*
   Copyright 2018 Digimarc, Inc

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class ConsoleLogger : ILogger
    {
        public ConsoleLogger(bool LogStack)
        {
            this.LogStack = LogStack;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        private static object _syncRoot = new object();

        public bool LogStack { get; }

        public void PrintHeader()
        {
            var stamp = DateTime.MinValue.ToString("o");
            for (int i = 0; i <= 6; i++)
            {
                printMsg(GetBadge((LogLevel)i), stamp, ((LogLevel)i).ToString());
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_syncRoot)
            {
                var color = Console.ForegroundColor;

                var badge = GetBadge(logLevel);
                var stamp = DateTime.UtcNow.ToString("o");
                printMsg(badge, stamp, formatter(state, exception));

                if (exception != null)
                {
                    printMsg(badge, stamp, exception.Message);
                    if (LogStack) { printMsg(badge, stamp, $"Stack:\n {exception.StackTrace}"); }
                }

                Console.ForegroundColor = color;
            }

        }

        private static (ConsoleColor, int, char) GetBadge(LogLevel logLevel) => logLevel switch
        {
            LogLevel.Trace => (ConsoleColor.Magenta, (int)logLevel, 'T'),
            LogLevel.Debug => (ConsoleColor.Gray, (int)logLevel, 'G'),
            LogLevel.Information => (ConsoleColor.Green, (int)logLevel, 'I'),
            LogLevel.Warning => (ConsoleColor.Yellow, (int)logLevel, 'W'),
            LogLevel.Error => (ConsoleColor.Red, (int)logLevel, 'E'),
            LogLevel.Critical => (ConsoleColor.DarkRed, (int)logLevel, 'C'),
            _ => (ConsoleColor.White, (int)logLevel, '?'),
        };


        static void printMsg((ConsoleColor color, int level, char ch) badge, string stamp, string message)
        {
            Console.ForegroundColor = badge.color;
            Console.Write($"{badge.level} ");
            Console.Write(badge.ch);

            Console.Write($" {stamp} ");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
        }
    }

    public class ConsoleLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Creates a new console logging provider
        /// </summary>
        /// <param name="logStack">If true, print stack traces for exceptions</param>
        /// <param name="printHeader">If true, print a set of sample logs at first startup</param>
        public ConsoleLoggerProvider(bool logStack, bool printHeader)
        {
            LogStack = logStack;
            PrintHeader = printHeader;
        }

        public bool LogStack { get; }
        public bool PrintHeader { get; }

        object syncRoot = new object();
        bool headerPrinted;

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new ConsoleLogger(LogStack);
            lock (syncRoot)
            {
                if (PrintHeader && !headerPrinted)
                {
                    headerPrinted = true;
                    logger.PrintHeader();
                }
            }
            return logger;
        }

        public void Dispose() { }
    }
}
