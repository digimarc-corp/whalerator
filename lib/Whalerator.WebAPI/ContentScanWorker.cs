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

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whalerator.Config;
using Whalerator.Content;
using Whalerator.Model;
using Whalerator.Queue;

namespace Whalerator.WebAPI
{
    public class ContentScanWorker : QueueWorker<Request>
    {
        private IContentScanner scanner;
        private RegistryAuthenticationDecoder authDecoder;

        public ContentScanWorker(ILogger<ContentScanWorker> logger, ConfigRoot config, IWorkQueue<Request> queue, IContentScanner scanner, IRegistryFactory regFactory,
            RegistryAuthenticationDecoder decoder) : base(logger, config, queue, regFactory)
        {
            this.scanner = scanner;
            authDecoder = decoder;
        }


        public override void DoRequest(Request request)
        {
            try
            {
                var auth = authDecoder.AuthenticateAsync(request.Authorization).Result;
                if (!auth.Succeeded)
                {
                    logger.LogWarning(auth.Failure, "Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    var registry = registryFactory.GetRegistry(auth.Principal.ToRegistryCredentials());

                    var imageSet = registry.GetImageSet(request.TargetRepo, request.TargetDigest, true);
                    if ((imageSet?.Images?.Count() ?? 0) != 1) { throw new Exception($"Couldn't find a valid image for {request.TargetRepo}:{request.TargetDigest}"); }

                    // if there is no cached result for this image/path, perform an index operation
                    if (scanner.GetPath(imageSet.Images.First(), request.Path) == null)
                    {
                        scanner.Index(registry, request.TargetRepo, imageSet.Images.First(), request.Path);
                        logger.LogInformation($"Completed indexing {request.TargetRepo}:{request.TargetDigest}:{request.Path}");
                    }
                    else
                    {
                        logger.LogInformation($"Request to index {request.TargetDigest}/{request.Path} in {request.TargetRepo} already has a cached result, and is being discarded.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Processing failed for work item\n {Newtonsoft.Json.JsonConvert.SerializeObject(request)}");
            }
        }
    }
}
