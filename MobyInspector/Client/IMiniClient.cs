using System;
using System.Collections.Generic;
using System.Net.Http;

namespace MobyInspector.Client
{
    public interface IMiniClient
    {
        HttpResponseMessage Get(Uri uri, string accept = null);
    }
}