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
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whalerator.Config;
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.WebAPI
{
    public class SecurityScanWorker : QueueWorker<Request>
    {
        private ISecurityScanner scanner;
        private RegistryAuthenticationDecoder authDecoder;

        public SecurityScanWorker(ILogger<SecurityScanWorker> logger, ServiceConfig config, IWorkQueue<Request> queue, ISecurityScanner scanner, IClientFactory clientFactory,
            RegistryAuthenticationDecoder decoder) : base(logger, config, queue, clientFactory)
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
                    logger.LogError(auth.Failure, "Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    var client = clientFactory.GetClient(auth.Principal.ToRegistryCredentials());

                    var imageSet = client.GetImageSet(request.TargetRepo, request.TargetDigest);
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
                            scanner.RequestScan(request.TargetRepo, imageSet.Images.First(), this.config.RegistryAlias?? this.config.Registry, request.Authorization);
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Processing failed for work item\n {Newtonsoft.Json.JsonConvert.SerializeObject(request)}");
            }
        }
    }
}
