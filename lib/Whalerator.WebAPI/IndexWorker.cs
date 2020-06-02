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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Queue;

namespace Whalerator.WebAPI
{
    public class IndexWorker : QueueWorker<IndexRequest>
    {
        private readonly IIndexStore indexStore;
        private readonly RegistryAuthenticationDecoder authDecoder;
        private readonly IAuthHandler authHandler;
        private readonly ICacheFactory cacheFactory;

        public IndexWorker(ILogger<IndexWorker> logger, ServiceConfig config, IWorkQueue<IndexRequest> queue, IClientFactory clientFactory, IIndexStore indexStore,
           RegistryAuthenticationDecoder authDecoder, IAuthHandler authHandler, ICacheFactory cacheFactory) : base(logger, config, queue, clientFactory)
        {
            this.indexStore = indexStore;
            this.authDecoder = authDecoder;
            this.authHandler = authHandler;
            this.cacheFactory = cacheFactory;
        }


        public override async Task DoRequestAsync(IndexRequest request)
        {
            try
            {
                var authResult = authDecoder.AuthenticateAsync(request.Authorization).Result;
                if (!authResult.Succeeded)
                {
                    logger.LogWarning(authResult.Failure, "Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    await authHandler.LoginAsync(authResult.Principal.ToRegistryCredentials());
                    var client = clientFactory.GetClient(authHandler);

                    var imageSet = await client.GetImageSetAsync(request.TargetRepo, request.TargetDigest);
                    if ((imageSet?.Images?.Count() ?? 0) != 1) { throw new Exception($"Couldn't find a valid image for {request.TargetRepo}:{request.TargetDigest}"); }
                    var image = imageSet.Images.First();

                    using (var @lock = await cacheFactory.Get<object>().TakeLockAsync($"idx:{image.Digest}", TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)))
                    {
                        if (!indexStore.IndexExists(image.Digest, request.TargetPaths.ToArray()))
                        {
                            var indexes = client.GetIndexes(request.TargetRepo, image, request.TargetPaths.ToArray());
                            indexStore.SetIndex(indexes, image.Digest, request.TargetPaths.ToArray());
                            logger.LogInformation($"Completed indexing {indexes.Max(i => i.Depth)} layer(s) from {request.TargetRepo}:{request.TargetDigest} {(request.TargetPaths.Count() == 0 ? "" : $"({string.Join(", ", request.TargetPaths)})")}");
                        }
                        else
                        {
                            logger.LogInformation($"Discarding duplicate index request for {request.TargetRepo}:{request.TargetDigest}");
                        }
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
