using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repository")]
    [Authorize]
    public class RepositoryController : Controller
    {
        private IRegistryFactory _RegFactory;
        private ILogger<RepositoryController> _Logger;

        public RepositoryController(IRegistryFactory regFactory, ILogger<RepositoryController> logger)
        {
            _RegFactory = regFactory;
            this._Logger = logger;
        }

        [HttpGet("files/{digest}/{*repository}")]
        public IActionResult GetFiles(string repository, string digest, int maxDepth = 0)
        {
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }
            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image identifier (digest or single-platform tag) is required."); }

            digest = Validate(digest);
            if (string.IsNullOrEmpty(digest)) { return BadRequest("Digest appears invalid."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var images = registryApi.GetImages(repository, digest);
                if (images.Count() > 1) { return BadRequest("Returned too many results; ensure image parameter is set to the digest of a specific image, not a tag."); }
                var files = registryApi.GetImageFiles(repository, images.First(), maxDepth == 0 ? int.MaxValue : maxDepth);

                return Ok(files);
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

        string Validate(string digest)
        {
            var validated = digest;
            var digestParts = digest.Trim(':').Split(':');
            if (digestParts.Length == 1 && digestParts[0].Length == 64) { validated = $"sha256:{digestParts[0]}"; }
            else if (digestParts.Length != 2) { validated = null; }

            return validated;
        }

        [HttpGet("file/{digest}/{*repository}")]
        public IActionResult GetFile(string repository, string digest, string path)
        {
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            digest = Validate(digest);
            if (string.IsNullOrEmpty(digest)) { return BadRequest("Digest appears invalid."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var image = registryApi.GetImages(repository, digest);

                if (image.Count() != 1) { return NotFound("No image was found with the given digest."); }

                // find a the actual layer first - this lets us work from cached file lists during the search (or build them if they're missing), and avoid expensive gunzipping until we have a hit
                var layer = registryApi.FindFile(repository, image.First(), path);
                if (layer == null) { return NotFound(); }

                var data = registryApi.GetFile(repository, layer, path);

                Response.Headers.Add("Content-Disposition", Path.GetFileName(path));
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

        /*
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
        }*/

        [HttpGet("tags/list/{*repository}")]
        public IActionResult GetTags(string repository)
        {
            _Logger.LogInformation($"Got request for {repository}");
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
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

        [HttpGet("tag/{tag}/{*repository}")]
        public IActionResult GetImages(string repository, string tag)
        {
            if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var images = registryApi.GetImages(repository, tag);

                var platforms = images.Select(i => i.Platform);
                var date = images.SelectMany(i => i.History.Select(h => h.Created)).Max();

                var result = new { Platforms = platforms, Date = date, Images = images, SetDigest = images.ToImageSetDigest() };

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