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
            var command = rawHistory.Created_By;
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

            /*
            if (TryMatch(command, HistoryType.Add, out var addCmd)) { command = addCmd; }
            else if (TryMatch(command, HistoryType.Arg, out var argCmd)) { command = argCmd; }
            else if (TryMatch(command, HistoryType.Cmd, out var argCmd)) { command = argCmd; }
            else if (TryMatch(command, HistoryType.Copy, out var argCmd)) { command = argCmd; }
            else if (TryMatch(command, HistoryType.Entrypoint, out var argCmd)) { command = argCmd; }
            else if (TryMatch(command, HistoryType.Env, out var argCmd)) { command = argCmd; }
            else if (TryMatch(command, HistoryType.Expose, out var argCmd)) { command = argCmd; }
            else if (TryMatch(command, HistoryType.Other, out var argCmd)) { command = argCmd; }

            var env = new Regex(@"^/bin/sh -c #\(nop\)\s+ENV\s+(.*)$", RegexOptions.Compiled);
            var add = new Regex(@"^/bin/sh -c #\(nop\)\s+ADD\s+(.*)$", RegexOptions.Compiled);
            var copy = new Regex(@"^/bin/sh -c #\(nop\)\s+COPY\s+(.*)$", RegexOptions.Compiled);
            var cmd = new Regex(@"^/bin/sh -c #\(nop\)\s+CMD\s+(.*)$", RegexOptions.Compiled);
            var workdir = new Regex(@"^/bin/sh -c #\(nop\)\s+WORKDIR\s+(.*)$", RegexOptions.Compiled);
            var entrypoint = new Regex(@"^/bin/sh -c #\(nop\)\s+ENTRYPOINT\s+(.*)$", RegexOptions.Compiled);
            var expose = new Regex(@"^/bin/sh -c #\(nop\)\s+EXPOSE\s+(.*)$", RegexOptions.Compiled);
            var arg = new Regex(@"^/bin/sh -c #\(nop\)\s+ARG\s+(.*)$", RegexOptions.Compiled);
            var run = new Regex(@"^/bin/sh -c\s+(?!#\(nop\))(.*)$", RegexOptions.Compiled);

            if (TryMatch(history, env)) { history.Type = HistoryType.Env; }
            else if (TryMatch(history, add)) { history.Type = HistoryType.Add; }
            else if (TryMatch(history, copy)) { history.Type = HistoryType.Copy; }
            else if (TryMatch(history, cmd)) { history.Type = HistoryType.Cmd; }
            else if (TryMatch(history, workdir)) { history.Type = HistoryType.Workdir; }
            else if (TryMatch(history, entrypoint)) { history.Type = HistoryType.Entrypoint; }
            else if (TryMatch(history, expose)) { history.Type = HistoryType.Expose; }
            else if (TryMatch(history, arg)) { history.Type = HistoryType.Arg; }
            else if (TryMatch(history, run)) { history.Type = HistoryType.Run; }

            return history;*/
        }

        private static (HistoryType type, string command) TryParse(string command)
        {
            foreach (HistoryType h in Enum.GetValues(typeof(HistoryType)))
            {
                if (h == HistoryType.Run)
                {
                    var match = new Regex(@"^/bin/sh -c\s+(?!#\(nop\))(.*)$", RegexOptions.Compiled).Match(command);
                    if (match.Success)
                    {
                        return (h, match.Groups[1].Value);
                    }
                }
                else
                {
                    var match = new Regex(@"^/bin/sh -c #\(nop\)\s+" + h.ToString().ToUpper() + @"\s+(.*)$").Match(command);
                    if (match.Success)
                    {
                        return (h, match.Groups[1].Value);
                    }
                }
            }

            return (HistoryType.Other, command);
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