using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whalerator.Client;

namespace Whalerator.WebAPI
{
    [Authorize]
    public abstract class WhaleratorControllerBase : ControllerBase
    {
        public WhaleratorControllerBase(ILoggerFactory logFactory, IAuthHandler auth)
        {
            LogFactory = logFactory;

            lazyAuthHandler = new Lazy<IAuthHandler>(() =>
            {
                var credentials = User.ToRegistryCredentials();
                credentials.Registry = RegistryCredentials.DockerHubAliases.Contains(credentials.Registry) ? RegistryCredentials.DockerHub : credentials.Registry.ToLowerInvariant();
                auth.LoginAsync(credentials).Wait();
                return auth;
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);
        }

        Lazy<IAuthHandler> lazyAuthHandler { get; }

        public ILoggerFactory LogFactory { get; }
        public IAuthHandler AuthHandler => lazyAuthHandler.Value;

        public RegistryCredentials RegistryCredentials => AuthHandler.RegistryCredentials;
    }
}
