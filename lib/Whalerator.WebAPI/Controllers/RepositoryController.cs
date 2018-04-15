using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Whalerator.WebAPI.Contracts;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repository")]
    [Authorize]
    public class RepositoryController : Controller
    {
        private ICache<IEnumerable<string>> _Cache;
        private IRegistryFactory _RegFactory;

        public RepositoryController(IRegistryFactory regFactory, ICache<IEnumerable<string>> cache)
        {
            _Cache = cache;
            _RegFactory = regFactory;
        }

        [HttpGet("file/{*repository}")]
        public IActionResult GetFile(string repository, string layer, string file)
        {
            var credentials = RegistryCredentials.FromClaimsPrincipal(User);
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials.Registry, credentials.Username, credentials.Password);
                var data = registryApi.GetFile(repository, new Model.Layer { Digest = layer }, file);
                
                Response.Headers.Add("Content-Disposition", Path.GetFileName(file));
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                return File(data, "application/octet-stream");
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

        [HttpGet("find/{*repository}")]
        public IActionResult Find(string repository, string image, string file)
        {
            var credentials = RegistryCredentials.FromClaimsPrincipal(User);
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            if (string.IsNullOrEmpty(image)) { return BadRequest("An image identifier (digest or single-platform tag) is required."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials.Registry, credentials.Username, credentials.Password);
                var images = registryApi.GetImages(repository, image);
                if (images.Count() > 1) { return BadRequest("Returned too many results; ensure image parameter is set to the digest of a specific image, not a tag."); }
                var layer = registryApi.FindFile(repository, images.First(), file);

                return layer == null ? NotFound() : (IActionResult)Ok(layer);
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

        [HttpGet("tags/{*repository}")]
        public IActionResult GetTags(string repository)
        {
            var credentials = RegistryCredentials.FromClaimsPrincipal(User);
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials.Registry, credentials.Username, credentials.Password);
                var tags = registryApi.GetTags(repository);

                return Ok(tags);
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

        [HttpGet("images/{*repository}")]
        public IActionResult GetImages(string repository, [FromQuery]string tag)
        {
            if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
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