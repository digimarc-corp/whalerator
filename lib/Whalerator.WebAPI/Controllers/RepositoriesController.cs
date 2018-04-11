using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Whalerator.Support;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/Repositories")]
    [Authorize]
    public class RepositoriesController : Controller
    {
        private IRegistryFactory _RegFactory;

        public RepositoriesController(IRegistryFactory regFactory)
        {
            _RegFactory = regFactory;
        }

        public IActionResult Get([FromQuery]string registry)
        {
            if (string.IsNullOrEmpty(registry))
            {
                return BadRequest(new { error = "No registry host supplied" });
            }
            else
            {
                string username = null;
                string password = null;
                if (User.Identity.AuthenticationType != "anonymous")
                {
                    var userRegisty = User.Claims.First(c => c.Type.Equals("Registry")).Value;
                    if (userRegisty.ToLowerInvariant() != registry.ToLowerInvariant())
                    {
                        throw new ArgumentException("The requested registry does not match the supplied credentials");
                    }
                    else
                    {
                        username = User.Claims.First(c => c.Type.Equals("Username")).Value;
                        password = User.Claims.First(c => c.Type.Equals("Password")).Value;
                    }
                }
                try
                {
                    var registryApi = _RegFactory.GetRegistry(registry, username, password);
                    var repos = registryApi.GetRepositories();

                    return Ok(repos);
                }
                catch (Client.AuthenticationException)
                {
                    return Unauthorized();
                }
            }
        }
    }
}