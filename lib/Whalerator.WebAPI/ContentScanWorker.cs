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
        private IContentScanner _Scanner;
        private RegistryAuthenticationDecoder _AuthDecoder;

        public ContentScanWorker(ILogger<ContentScanWorker> logger, ConfigRoot config, IWorkQueue<Request> queue, IContentScanner scanner, IRegistryFactory regFactory,
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
                    _Logger.LogWarning(auth.Failure, "Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    var registry = _RegistryFactory.GetRegistry(auth.Principal.ToRegistryCredentials());

                    var imageSet = registry.GetImageSet(request.TargetRepo, request.TargetDigest, true);
                    if (imageSet.Images.Count() != 1) { throw new Exception($"Couldn't find a valid image for {request.TargetRepo}:{request.TargetDigest}"); }

                    // if there is no cached result for this image/path, perform an index operation
                    if (_Scanner.GetPath(imageSet.Images.First(), request.Path) == null)
                    {
                        _Scanner.Index(registry, request.TargetRepo, imageSet.Images.First(), request.Path);
                        _Logger.LogInformation($"Completed indexing {request.TargetRepo}:{request.TargetDigest}:{request.Path}");
                    }
                    else
                    {
                        _Logger.LogInformation($"Request to index {request.TargetDigest}/{request.Path} in {request.TargetRepo} already has a cached result, and is being discarded.");
                    }
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, $"Processing failed for work item\n {Newtonsoft.Json.JsonConvert.SerializeObject(request)}");
            }
        }
    }
}
