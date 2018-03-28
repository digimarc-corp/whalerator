using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MobyInspector
{
    /// <summary>
    /// Maintains a list of Authorization header values with an attached domain scope, and applies the appropriate header when sending to that domain.
    /// </summary>
    public class ScopedAuthenticationClientHandler : HttpClientHandler
    {
        /// <summary>
        /// Takes a domain/scope and an authorization header, and adds it to the handler. If an existing authorization has the same scope, it will be replaced.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="authorization"></param>
        public void AddScope(string scope, string authorization)
        {
            Authorizations[scope] = AuthenticationHeaderValue.Parse(authorization);
        }

        public Dictionary<string, AuthenticationHeaderValue> Authorizations { get; set; } = new Dictionary<string, AuthenticationHeaderValue>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // See if the request has an authorize header
            var auth = request.Headers.Authorization;
            if (auth == null)
            {
                var domain = request.RequestUri.Host;

                //see if we have a matching authorization and take the longest/most specific
                var scope = Authorizations.Where(a => domain.EndsWith(a.Key)).OrderBy(a => a.Key.Length);
                if (scope.Count() > 0)
                {
                    request.Headers.Authorization = scope.First().Value;
                }
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
