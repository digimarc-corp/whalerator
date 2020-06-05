using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public class Options
    {
        [Option('x', "exit", Required = false, HelpText = "Exit after loading configuration and processing rescan, if requested.")]
        public bool Exit { get; set; }

        [Option('r', "rescan", Required = false, HelpText = "Enumerate all repositories, images, and tags in the configured registry, and submit for indexing and/or scanning.")]
        public bool Rescan { get; set; }

        [Option('b', "nobanner", Required = false, HelpText = "Skip printing startup banner.")]
        public bool NoBanner { get; set; }
    }
}
