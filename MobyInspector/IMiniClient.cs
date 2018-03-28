using System;
using System.Collections.Generic;
using System.Net.Http;

namespace MobyInspector
{
    public interface IMiniClient
    {
        HttpResponseMessage Get(Uri uri, string accept = null);
    }
}