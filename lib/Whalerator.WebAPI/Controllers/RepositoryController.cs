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
using Org.BouncyCastle.Ocsp;
using StackExchange.Redis;
using Whalerator.Client;
using Whalerator.Content;
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repository")]
    [Authorize]
    public class RepositoryController : WhaleratorControllerBase
    {
        private IClientFactory clientFactory;
        private ILogger<RepositoryController> logger;
        private readonly IIndexStore indexStore;
        private readonly IWorkQueue<IndexRequest> indexQueue;
        private ISecurityScanner secScanner;

        public RepositoryController(ILoggerFactory logFactory, IAuthHandler auth, IClientFactory clientFactory, IIndexStore indexStore, IWorkQueue<IndexRequest> indexQueue, ISecurityScanner secScanner = null) :
            base(logFactory, auth)
        {
            this.clientFactory = clientFactory;
            this.logger = logFactory.CreateLogger<RepositoryController>();
            this.indexStore = indexStore;
            this.indexQueue = indexQueue;
            this.secScanner = secScanner;
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
            if (secScanner == null) { return StatusCode(503, "Security scanning is not currently enabled."); }

            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image digest is required."); }
            if (!digest.IsDigest()) { return BadRequest("Digest appears invalid."); }

            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var imageSet = client.GetImageSet(repository, digest);

                if (imageSet.Images.Count() != 1) { return NotFound("No image was found with the given digest."); }

                var scanResult = secScanner.GetScan(imageSet.Images.First());

                if (scanResult == null)
                {
                    secScanner.Queue.TryPush(new Security.ScanRequest
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
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
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
        /// <param name="path">Optional target path. Indexing will halt if the target is found, and return only those layers needed to reach the target.</param>
        /// <returns></returns>
        [HttpGet("files/{digest}/{*repository}")]
        public IActionResult GetFiles(string repository, string digest, string path)
        {
            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image digest is required."); }
            if (!digest.IsDigest()) { return BadRequest("Digest appears invalid."); }

            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var imageSet = client.GetImageSet(repository, digest);

                if (imageSet.Images.Count() != 1) { return NotFound("No image was found with the given digest."); }

                // if we have a complete index, return it
                if (indexStore.IndexExists(digest))
                {
                    return Ok(indexStore.GetIndex(digest));
                }
                // otherwise, if they've requested a targeted index, look for that
                else if (indexStore.IndexExists(digest, path))
                {
                    return Ok(indexStore.GetIndex(digest, path));
                }
                // no dice, queue an indexing request
                else
                {
                    var request = new IndexRequest
                    {
                        Authorization = Request.Headers["Authorization"],
                        CreatedTime = DateTime.UtcNow,
                        TargetRepo = repository,
                        TargetDigest = digest,
                        TargetPath = path
                    };
                    if (!indexQueue.Contains(request)) { indexQueue.Push(request); }

                    return StatusCode(202, new { Status = RequestStatus.Pending });
                }
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
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

        [HttpGet("file/{digest}/{*repository}")]
        public IActionResult GetFile(string repository, string digest, string path)
        {
            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image digest is required."); }
            if (!digest.IsDigest()) { return BadRequest("Digest appears invalid."); }

            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);

                var layer = client.GetLayer(repository, digest);
                var result = client.GetFile(repository, layer, path);

                Response.Headers.Add("Content-Disposition", Path.GetFileName(path));
                Response.Headers.Add("X-Content-Type-Options", "nosniff");
                return File(result, "application/octet-stream");
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
            }
            catch (Client.NotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Client.AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [HttpGet("tags/list/{*repository}")]
        public IActionResult GetTags(string repository)
        {
            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var tags = client.GetTags(repository);
                var permissions = client.GetPermissions(repository);

                return Ok(new TagSet { Tags = tags, Permissions = permissions });
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
            }
            catch (Client.NotFoundException)
            {
                return NotFound();
            }
            catch (Client.AuthenticationException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                return StatusCode(555);
            }
        }

        [HttpGet("tag/{tag}/{*repository}")]
        public IActionResult GetImages(string repository, string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var imageSet = client.GetImageSet(repository, tag);

                return imageSet == null ? (IActionResult)NotFound() : Ok(imageSet);
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
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
            try
            {
                logger.LogInformation($"Preparing to delete {repository}/{digest}");
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var permissions = client.GetPermissions(repository);
                if (permissions != Permissions.Admin) { return Unauthorized(); }

                client.DeleteImage(repository, digest);
                return Ok();
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
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
            try
            {
                if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var digest = client.GetTagDigest(repository, tag);

                return string.IsNullOrEmpty(digest) ? (IActionResult)NotFound() : Ok(digest);
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
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
