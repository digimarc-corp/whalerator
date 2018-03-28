using System.Net.Http.Headers;

namespace MobyInspector.Client
{
    public interface ITokenSource
    {
        string GetToken(AuthenticationHeaderValue authenticateHeader);
        string GetToken(string realm, string service, string scope);
    }
}