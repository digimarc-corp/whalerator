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
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;
using StackExchange.Redis;
using Whalerator.Client;
using Whalerator.Content;
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;
using YamlDotNet.Core.Tokens;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repository")]
    [Authorize]
    public class RepositoryController : WhaleratorControllerBase
    {
        private readonly IClientFactory clientFactory;
        private readonly ILogger<RepositoryController> logger;
        private readonly IIndexStore indexStore;
        private readonly IWorkQueue<IndexRequest> indexQueue;
        private readonly ISecurityScanner secScanner;

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
        public async Task<IActionResult> GetSecScanAsync(string repository, string digest)
        {
            if (secScanner == null) { return StatusCode(503, "Security scanning is not currently enabled."); }

            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image digest is required."); }
            if (!digest.IsDigest()) { return BadRequest("Digest appears invalid."); }

            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var imageSet = await client.GetImageSetAsync(repository, digest);

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
        /// <param name="targets">Optional target paths. Indexing will halt once all targets are found, and return only those layers needed to reach the deepest target.</param>
        /// <returns></returns>
        [HttpGet("files/{digest}/{*repository}")]
        public async Task<IActionResult> GetFilesAsync(string repository, string digest, string targets)
        {
            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image digest is required."); }
            if (!digest.IsDigest()) { return BadRequest("Digest appears invalid."); }

            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var targetList = string.IsNullOrEmpty(targets) ? Array.Empty<string>() : targets.Split(';');

                var client = clientFactory.GetClient(AuthHandler);
                var imageSet = await client.GetImageSetAsync(repository, digest);

                if (imageSet.Images.Count() != 1) { return NotFound("No image was found with the given digest."); }

                // if we have a complete index, return it
                if (indexStore.IndexExists(digest))
                {
                    return Ok(indexStore.GetIndex(digest));
                }
                // otherwise, if they've requested a targeted index, look for that
                else if (indexStore.IndexExists(digest, targetList))
                {
                    return Ok(indexStore.GetIndex(digest, targetList));
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
                        TargetPaths = targetList
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
        public async Task<IActionResult> GetFileAsync(string repository, string digest, string path)
        {
            if (string.IsNullOrEmpty(digest)) { return BadRequest("An image digest is required."); }
            if (!digest.IsDigest()) { return BadRequest("Digest appears invalid."); }

            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);

                var layer = await client.GetLayerAsync(repository, digest);

                var result = await client.GetFileAsync(repository, layer, path);
                IContentTypeProvider ct = new FileExtensionContentTypeProvider();

                if (!ct.TryGetContentType(path, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return File(result, contentType);
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
        public async Task<IActionResult> GetTagsAsync(string repository)
        {
            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var permissions = await client.GetPermissionsAsync(repository);
                var tags = permissions >= Permissions.Pull ? client.GetTags(repository) : Array.Empty<string>();

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
        }

        [HttpGet("tag/{tag}/{*repository}")]
        public async Task<IActionResult> GetImagesAsync(string repository, string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var imageSet = await client.GetImageSetAsync(repository, tag);

                return imageSet == null ? (IActionResult)NotFound() : Ok(imageSet);
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
            }
            catch (RegistryException ex)
            {
                return StatusCode(502, ex.Message);
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
        public async Task<IActionResult> DeleteImageSet(string repository, string digest)
        {
            try
            {
                logger.LogInformation($"Preparing to delete {repository}/{digest}");
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var permissions = await client.GetPermissionsAsync(repository);
                if (permissions != Permissions.Admin) { return Unauthorized(); }

                await client.DeleteImageAsync(repository, digest);
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
        public async Task<IActionResult> GetImagesDigestAsync(string repository, string tag)
        {
            try
            {
                if (string.IsNullOrEmpty(tag)) { return BadRequest("A tag is required."); }
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var digest = await client.GetTagDigestAsync(repository, tag);

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
