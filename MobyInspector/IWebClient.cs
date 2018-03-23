using System;

namespace MobyInspector
{
    public interface IWebClient
    {
        string Password { get; set; }
        string Username { get; set; }

        string Get(Uri uri);
    }
}