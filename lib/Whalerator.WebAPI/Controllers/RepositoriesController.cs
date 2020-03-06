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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Whalerator.Client;
using Whalerator.Model;
using Whalerator.Support;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/repositories")]
    [Authorize]
    public class RepositoriesController : Controller
    {
        private IRegistryFactory regFactory;

        public RepositoriesController(IRegistryFactory regFactory)
        {
            this.regFactory = regFactory;
        }

        [HttpGet("list")]
        public IActionResult Get(bool empties = false)
        {
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = regFactory.GetRegistry(credentials);

                // Tag count also serves as workaround for https://github.com/docker/distribution/issues/2434
                var repos = registryApi.GetRepositories(empties).OrderBy(r => r.Name);

                return Ok(repos);
            }
            catch (AuthenticationException)
            {
                return Unauthorized();
            }
        }

        [HttpDelete("{*repository}")]
        public IActionResult Delete(string repository)
        {
            var credentials = User.ToRegistryCredentials();
            if (string.IsNullOrEmpty(credentials.Registry)) { return BadRequest("Session is missing registry information. Try creating a new session."); }

            try
            {
                var registryApi = regFactory.GetRegistry(credentials);
                var permissions = registryApi.GetPermissions(repository);
                if (permissions != Permissions.Admin) { return Unauthorized(); }

                registryApi.DeleteRepository(repository);
                return Ok();
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