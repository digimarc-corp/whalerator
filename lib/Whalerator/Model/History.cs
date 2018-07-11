using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Whalerator.Model
{
    public class History
    {
        public string Command { get; set; }
        public DateTime Created { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public HistoryType Type { get; set; }
        public string ShortCommand { get; set; }
        public string CommandArgs { get; set; }

        public static History From(Data.History rawHistory)
        {
            // null Created_By usually means the image was squashed
            var command = rawHistory.Created_By ?? string.Empty;
            string args = null;

            var argsm = new Regex(@"^\|\d(.+)(/bin/sh.*)$", RegexOptions.Compiled).Match(command);
            if (argsm.Success)
            {
                args = argsm.Groups[1].Value;
                command = argsm.Groups[2].Value;
            }

            var parsed = TryParse(command);

            return new History
            {
                Command = rawHistory.Created_By,
                CommandArgs = args,
                Created = rawHistory.Created,
                ShortCommand = parsed.command,
                Type = parsed.type
            };
        }

        private static (HistoryType type, string command) TryParse(string command)
        {
            Match match;
            foreach (HistoryType h in Enum.GetValues(typeof(HistoryType)))
            {
                if (h == HistoryType.Run)
                {
                    continue; // must check for Run commands last
                }
                else
                {
                    match = new Regex(@"^/bin/sh -c #\(nop\)\s+" + h.ToString().ToUpper() + @"\s+(.*)$").Match(command);
                    if (match.Success)
                    {
                        return (h, match.Groups[1].Value);
                    }
                }
            }

            // nothing more specific matched, try Run
            match = new Regex(@"^/bin/sh -c\s+(?!#\(nop\))(.*)$", RegexOptions.Compiled).Match(command);
            if (match.Success)
            {
                return (HistoryType.Run, match.Groups[1].Value);
            }
            else
            {
                return (HistoryType.Other, command);
            }
        }

        private static bool TryMatch(History history, Regex regex, int cmdGroup = 1)
        {
            var match = regex.Match(history.Command);
            if (match.Success)
            {
                history.ShortCommand = match.Groups[cmdGroup].Value;
                return true;
            }
            else { return false; }
        }
    }
}