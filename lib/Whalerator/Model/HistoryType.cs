using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Model
{
    public enum HistoryType
    {
        Other,
        Add,
        Copy,
        Cmd,
        Env,
        Entrypoint,
        Workdir,
        Run,
        Arg,
        Expose
    }
}
