using System;
using System.Net.Http.Headers;

namespace Whalerator.Client
{
    public interface IAuthHandler
    {
        bool AnonymousMode { get; }
        AuthenticationHeaderValue GetAuthorization(string scope);
        bool Authorize(string scope);
        bool HasAuthorization(string scope);
        void Login(string registryHost, string username = null, string password = null);
        string ParseScope(Uri uri);
        (string realm, string service, string scope) ParseWwwAuthenticate(AuthenticationHeaderValue header);
        bool TryParseScope(Uri uri, out string scope);
        bool UpdateAuthorization(string scope);
    }
}