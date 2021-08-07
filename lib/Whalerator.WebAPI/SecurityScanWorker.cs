﻿/*
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
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.WebAPI
{
    public class SecurityScanWorker : QueueWorker<ScanRequest>
    {
        private readonly ISecurityScanner scanner;
        private readonly IAuthHandler authHandler;
        private readonly RegistryAuthenticationDecoder authDecoder;

        public SecurityScanWorker(ILogger<SecurityScanWorker> logger, ServiceConfig config, IWorkQueue<ScanRequest> queue, ISecurityScanner scanner, IClientFactory clientFactory,
            RegistryAuthenticationDecoder authDecoder, IAuthHandler authHandler) : base(logger, config, queue, clientFactory)
        {
            this.scanner = scanner;
            this.authDecoder = authDecoder;
            this.authHandler = authHandler;
        }

        public override async Task DoRequestAsync(ScanRequest request)
        {
            try
            {
                RegistryCredentials credentials = null;

                // if the request was submitted by a user, it must have auth info included
                if (!string.IsNullOrEmpty(request.Authorization))
                {
                    var authResult = authDecoder.AuthenticateAsync(request.Authorization).Result;
                    if (authResult.Succeeded)
                    {
                        credentials = authResult.Principal.ToRegistryCredentials();
                    }
                }
                // if the request came via an event sink, there is no auth provided, and we need to have a default user configured
                else
                {
                    credentials = config.GetCatalogCredentials() ?? throw new ArgumentException("The indexing request had no included authorization, and no default catalog user is configured.");
                }

                if (credentials == null)
                {
                    logger.LogError("Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    await authHandler.LoginAsync(credentials);
                    var scope = authHandler.RepoPullScope(request.TargetRepo);
                    if (await authHandler.AuthorizeAsync(scope))
                    {
                        var proxyAuth = authHandler.TokensRequired ? $"Bearer {(await authHandler.GetAuthorizationAsync(scope)).Parameter}" : string.Empty;
                        var client = clientFactory.GetClient(authHandler);

                        var imageSet = await client.GetImageSetAsync(request.TargetRepo, request.TargetDigest);
                        if ((imageSet?.Images?.Count() ?? 0) != 1) { throw new Exception($"Couldn't find a valid image for {request.TargetRepo}:{request.TargetDigest}"); }

                        var scanResult = scanner.GetScan(imageSet.Images.First());
                        if (scanResult == null)
                        {
                            if (request.Submitted)
                            {
                                // we've already submitted this one to the scanner, just sleep on it for a few seconds
                                System.Threading.Thread.Sleep(2000);
                            }
                            else
                            {
                                var host = authHandler.GetRegistryHost();
                                scanner.RequestScan(request.TargetRepo, imageSet.Images.First(), host, proxyAuth);
                                logger.LogInformation($"Submitted {request.TargetRepo}:{request.TargetDigest} to {scanner.GetType().Name} for analysis.");
                                request.Submitted = true;
                            }
                            queue.Push(request);
                        }
                        else
                        {
                            logger.LogInformation($"Got latest {scanner.GetType().Name} scan for {request.TargetRepo}:{request.TargetDigest}");
                        }
                    }
                    else
                    {
                        logger.LogError($"Failed to get pull authorization for {request.TargetRepo}");
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
