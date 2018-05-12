using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Whalerator.Client;
using Whalerator.Support;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repositories")]
    [Authorize]
    public class RepositoriesController : Controller
    {
        private IRegistryFactory _RegFactory;

        public RepositoriesController(IRegistryFactory regFactory)
        {
            _RegFactory = regFactory;
        }

        [HttpGet("list")]
        public IActionResult Get()
        {
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
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