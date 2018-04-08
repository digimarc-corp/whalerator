using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Whalerator.Client
{
    public interface IAuthHandler2
    {
        string Username { get; set; }
        string Password { get; set; }
        AuthenticationHeaderValue GetAuthorization(string service, string scope);
        (string realm, string service, string scope) ParseWwwAuthenticate(AuthenticationHeaderValue header);
        bool TryParseScope(Uri uri, out string scope);
        bool HasAuthorization(string service, string scope);
        bool UpdateAuthentication(string realm, string service, string scope);
    }
}
