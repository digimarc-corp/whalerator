using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Whalerator.WebAPI.Contracts;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/image")]
    [Authorize]
    public class ImageController : Controller
    {
        private IRegistryFactory _RegFactory;

        public ImageController(IRegistryFactory regFactory)
        {
            _RegFactory = regFactory;
        }

        [HttpGet("{*repository}")]
        public IActionResult Get(string repository, [FromQuery]string tag)
        {
            var credentials = RegistryCredentials.FromClaimsPrincipal(User);
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials.Registry, credentials.Username, credentials.Password);
                var images = registryApi.GetImages(repository, tag);

                var platforms = images.Select(i => i.Platform);
                var date = images.SelectMany(i => i.History.Select(h => h.Created)).Max();

                var result = new { Platforms = platforms, Date = date, Images = images };

                return Ok(result);
            }
            catch (Client.NotFoundException)
            {
                return NotFound();
            }
            catch (Client.AuthenticationException)
            {
                return Unauthorized();
            }
        }
    }
}