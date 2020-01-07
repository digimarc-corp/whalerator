/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Whalerator.Content;
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repository")]
    [Authorize]
    public class RepositoryController : Controller
    {
        private IRegistryFactory _RegFactory;
        private ILogger<RepositoryController> _Logger;
        private ISecurityScanner _SecScanner;
        private IContentScanner _ContentScanner;

        public RepositoryController(IRegistryFactory regFactory, ILogger<RepositoryController> logger, ISecurityScanner secScanner = null, IContentScanner contentScanner = null)
        {
            _RegFactory = regFactory;
            _Logger = logger;
            _SecScanner = secScanner;
            _ContentScanner = contentScanner;
        }

        string Validate(string digest)
        {
            var validated = digest;
            var digestParts = digest.Trim(':').Split(':');
            if (digestParts.Length == 1 && digestParts[0].Length == 64) { validated = $"sha256:{digestParts[0]}"; }
            else if (digestParts.Length != 2) { validated = null; }

            return validated;
        }

        /// <summary>
        /// Check for security scan data for the given image. If no scan data is available, a scan will be queued to run later.
        /// </summary>
        /// <param name="repository">Registry repo to search</param>
        /// <param name="digest">Manifest digest. Must be for a discrete image, not a multiplatform image.</param>
        /// <returns></returns>
        [HttpGet("sec/{digest}/{*repository}")]
        public IActionResult GetSecScan(string repository, string digest)
        {
            if (_SecScanner == null) { return StatusCode(503, "Security scanning is not currently enabled."); }

            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            digest = Validate(digest);
            if (string.IsNullOrEmpty(digest)) { return BadRequest("Digest appears invalid."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var imageSet = registryApi.GetImageSet(repository, digest, true);

                if (imageSet.Images.Count() != 1) { return NotFound("No image was found with the given digest."); }

                var scanResult = _SecScanner.GetScan(imageSet.Images.First());

                if (scanResult == null)
                {
                    _SecScanner.Queue.Push(new Security.Request
                    {
                        Authorization = Request.Headers["Authorization"],
                        CreatedTime = DateTime.UtcNow,
                        TargetRepo = repository,
                        TargetDigest = digest
                    });
                    return StatusCode(202, new Security.Result { Status = RequestStatus.Pending });
                }
                else
                {
                    return Ok(scanResult);
                }
            }
            catch (HttpRequestException)
            {
                return StatusCode(503, "Scanner API is not available.");
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

        /// <summary>
        /// Gets a file listing from a specific image manifest
        /// </summary>
        /// <param name="repository">Registry repo to search</param>
        /// <param name="digest">Manifest digest</param>
        /// <param name="maxDepth">Maximum number of layers to descend</param>
        /// <returns></returns>
        [HttpGet("files/{digest}/{*repository}")]
        public IActionResult GetFiles(string repository, string digest) => GetFile(repository, digest, null);

        [HttpGet("file/{digest}/{*repository}")]
        public IActionResult GetFile(string repository, string digest, string path)
        {
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }
            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image identifier (digest or single-platform tag) is required."); }

            digest = Validate(digest);
            if (string.IsNullOrEmpty(digest)) { return BadRequest("Digest appears invalid."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var imageSet = registryApi.GetImageSet(repository, digest, true);

                if (imageSet.Images.Count() != 1) { return NotFound("No image was found with the given digest."); }

                var result = _ContentScanner.GetPath(imageSet.Images.First(), path);

                if (result == null)
                {
                    _ContentScanner.Queue.TryPush(new Whalerator.Content.Request
                    {
                        Authorization = Request.Headers["Authorization"],
                        CreatedTime = DateTime.UtcNow,
                        TargetRepo = repository,
                        TargetDigest = digest,
                        Path = path,
                    });

                    return StatusCode(202, new Content.Result { Status = RequestStatus.Pending });
                }
                else
                {
                    if (!result.Exists)
                    {
                        return NotFound();
                    }
                    else
                    {
                        if (result.Content != null)
                        {
                            Response.Headers.Add("Content-Disposition", Path.GetFileName(path));
                            Response.Headers.Add("X-Content-Type-Options", "nosniff");
                            return File(result.Content, "application/octet-stream");
                        }
                        else
                        {
                            return Ok(result.Children);
                        }
                    }
                }

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
                var permissions = registryApi.GetPermissions(repository);

                return Ok(new TagSet { Tags = tags, Permissions = permissions });
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
                var imageSet = registryApi.GetImageSet(repository, tag, false);

                return Ok(imageSet);
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

        /// <summary>
        /// Delete an image and all corresponding tags.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="digest"></param>
        /// <returns></returns>
        [HttpDelete("digest/{digest}/{*repository}")]
        public IActionResult DeleteImageSet(string repository, string digest)
        {
            _Logger.LogInformation($"Preparing to delete {repository}/{digest}");
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var permissions = registryApi.GetPermissions(repository);
                if (permissions != Permissions.Admin) { return Unauthorized(); }

                registryApi.DeleteImage(repository, digest);
                return Ok();
            }
            catch (Client.RegistryException ex)
            {
                return StatusCode(405, ex.Message);
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

        /// <summary>
        /// Useful when trying to corellate multiple tags to a single image set, without resending the complete image set over the wire repeatedly.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        [HttpGet("digest/{tag}/{*repository}")]
        public IActionResult GetImagesDigest(string repository, string tag)
        {
            if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = _RegFactory.GetRegistry(credentials);
                var imageSet = registryApi.GetImageSet(repository, tag, false);

                return Ok(imageSet.SetDigest);
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
