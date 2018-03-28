using System;
using System.Net.Http.Headers;

namespace MobyInspector.Client
{
    public interface IAuthHandler
    {
        // gets current auth info for a uri
        AuthenticationHeaderValue GetAuthorization(Uri uri);

        // update auth for the given uri, and returns a value indicating success
        bool HandleUnauthorized(Uri uri, HttpResponseHeaders headers);
    }
}