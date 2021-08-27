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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Model;
using Whalerator.Support;

namespace Whalerator.WebAPI.Controllers
{
    [Route(Startup.ApiBase + "[controller]")]
    [Authorize]
    public class RepositoriesController : WhaleratorControllerBase
    {
        private readonly ServiceConfig config;
        private IClientFactory clientFactory;

        public RepositoriesController(ILoggerFactory logFactory, ServiceConfig config, IAuthHandler auth, IClientFactory regFactory) : base(logFactory, auth)
        {
            this.config = config;
            this.clientFactory = regFactory;
        }

        [HttpGet("banner")]
        [Produces("text/markdown")]
        public IActionResult GetBanner()
        {
            try
            {
                var banner = Banners.ReadBanner(config.CatalogBanner);
                var stream = new MemoryStream();
                using (var sr = new StreamWriter(stream, leaveOpen: true))
                {
                    sr.Write(banner);
                }
                stream.Seek(0, SeekOrigin.Begin);
                return File(stream, "text/markdown");

            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("list")]
        [Produces("application/json")]
        public IActionResult Get()
        {
            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                // Tag count also serves as workaround for https://github.com/docker/distribution/issues/2434
                return Ok(client.GetRepositories().Where(r => r.Permissions >= Permissions.Pull).OrderBy(r => r.Name));
            }
            catch (AggregateException ex)
            {
                IActionResult result = null;
                ex.Handle((ex) =>
                {
                    result = ex switch
                    {
                        AuthenticationException => Unauthorized(),
                        RedisConnectionException => StatusCode(503, "Cannot access cache"),
                        _ => null
                    };
                    return result is not null;
                });
                return result;
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [HttpDelete("{*repository}")]
        public async Task<IActionResult> DeleteAsync(string repository)
        {
            try
            {
                if (string.IsNullOrEmpty(RegistryCredentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

                var client = clientFactory.GetClient(AuthHandler);
                var permissions = await client.GetPermissionsAsync(repository);
                if (permissions != Permissions.Admin) { return Unauthorized(); }

                await client.DeleteRepositoryAsync(repository);
                return Ok();
            }
            catch (AggregateException ex)
            {
                IActionResult result = null;
                ex.Handle((ex) =>
                {
                    result = ex switch
                    {
                        AuthenticationException => Unauthorized(),
                        RedisConnectionException => StatusCode(503, "Cannot access cache"),
                        NotFoundException => NotFound(),
                        _ => null
                    };
                    return result is not null;
                });
                return result;
            }
            catch (RedisConnectionException)
            {
                return StatusCode(503, "Cannot access cache");
            }
            catch (RegistryException ex)
            {
                return StatusCode(405, ex.Message);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }
    }
}