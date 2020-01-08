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
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.WebAPI
{
    public class SecurityScanWorker : QueueWorker<Request>
    {
        private ISecurityScanner _Scanner;
        private RegistryAuthenticationDecoder _AuthDecoder;

        public SecurityScanWorker(ILogger<SecurityScanWorker> logger, ConfigRoot config, IWorkQueue<Request> queue, ISecurityScanner scanner, IRegistryFactory regFactory,
            RegistryAuthenticationDecoder decoder) : base(logger, config, queue, regFactory)
        {
            _Scanner = scanner;
            _AuthDecoder = decoder;
        }


        public override void DoRequest(Request request)
        {
            try
            {
                var auth = _AuthDecoder.AuthenticateAsync(request.Authorization).Result;
                if (!auth.Succeeded)
                {
                    _Logger.LogError(auth.Failure, "Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    var registry = _RegistryFactory.GetRegistry(auth.Principal.ToRegistryCredentials());

                    var imageSet = registry.GetImageSet(request.TargetRepo, request.TargetDigest, true);
                    if ((imageSet?.Images?.Count() ?? 0) != 1) { throw new Exception($"Couldn't find a valid image for {request.TargetRepo}:{request.TargetDigest}"); }

                    _Scanner.RequestScan(registry, request.TargetRepo, imageSet.Images.First());
                    _Logger.LogInformation($"Completed submitting {request.TargetRepo}:{request.TargetDigest} to {_Scanner.GetType().Name} for analysis.");
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, $"Processing failed for work item\n {Newtonsoft.Json.JsonConvert.SerializeObject(request)}");
            }
        }
    }
}
