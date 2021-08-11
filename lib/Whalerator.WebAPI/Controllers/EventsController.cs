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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Ocsp;
using Whalerator.Config;
using Whalerator.Data;
using Whalerator.DockerClient;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.WebAPI.Controllers
{
    [Route(Startup.ApiBase + "[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ServiceConfig config;
        private readonly IWorkQueue<IndexRequest> indexQueue;
        private readonly ISecurityScanner secScanner;
        private readonly ILogger<EventsController> logger;

        public EventsController(ServiceConfig config, IWorkQueue<IndexRequest> indexQueue, ISecurityScanner secScanner, ILogger<EventsController> logger)
        {
            this.config = config;
            this.indexQueue = indexQueue;
            this.secScanner = secScanner;
            this.logger = logger;
        }

        [HttpPost]
        public IActionResult PostAsync(EventEnvelope envelope)
        {
            if (config.EventSink)
            {
                // Authorization in this controller is entirely static, as this is the only mode Docker Registry supports
                if (!string.IsNullOrEmpty(config.EventAuthorization))
                {
                    if (!Request.Headers.ContainsKey("Authorization") ||
                        Request.Headers["Authorization"] != config.EventAuthorization)
                    {
                        return Unauthorized();
                    }
                }

                if (envelope != null)
                {
                    foreach (var e in envelope.Events)
                    {
                        switch (e.Action.ToLowerInvariant())
                        {
                            case "push":
                                if (LocalDockerClient.SupportedManifestTypes.Contains(e.Target.MediaType))
                                {
                                    if (config.Documents?.Count > 0) { RequestIndex(e); }
                                    if (config.Vulnerabilities) { RequestScan(e); }
                                }
                                break;
                            default:
                                logger.LogInformation($"Ignoring  event {e.Id} - {e.Action} {e.Target?.MediaType} {e.Target?.Digest}");
                                break;
                        }
                    }

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            else
            {
                return StatusCode(503, "Event sink service is not enabled");
            }
        }

        private void RequestScan(Event e)
        {
            secScanner.Queue.TryPush(new Security.ScanRequest
            {
                CreatedTime = DateTime.UtcNow,
                TargetRepo = e.Target.Repository,
                TargetDigest = e.Target.Digest
            });
            logger.LogInformation($"Submitted scan request for event {e.Id} - {e.Action} {e.Target.MediaType} {e.Target.Digest}");
        }

        private void RequestIndex(Event e)
        {
            // we only check for an existing index request, not an actual index. Letting the indexer sort it out later should be cheap
            var request = new IndexRequest
            {
                CreatedTime = DateTime.UtcNow,
                TargetRepo = e.Target.Repository,
                TargetDigest = e.Target.Digest,
                TargetPaths = config.DeepIndexing ? null : config.Documents
            };
            if (!indexQueue.Contains(request)) { indexQueue.Push(request); }
            logger.LogInformation($"Submitted indexing request for event {e.Id} - {e.Action} {e.Target.MediaType} {e.Target.Digest}");
        }
    }
}
