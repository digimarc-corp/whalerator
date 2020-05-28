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
using Newtonsoft.Json.Linq;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.DockerClient;
using Whalerator.Model;
using Whalerator.Queue;
using Whalerator.Security;

namespace Whalerator.Support
{
    public class ClairScanner : ISecurityScanner
    {
        private ILogger logger;
        private ServiceConfig config;
        private IClairAPI clair;
        private ICacheFactory cacheFactory;
        private readonly IAuthHandler auth;

        public IWorkQueue<ScanRequest> Queue { get; private set; }

        public ClairScanner(ILogger<ClairScanner> logger, ServiceConfig config, IClairAPI clair, ICacheFactory cacheFactory, IWorkQueue<ScanRequest> queue)
        {
            this.logger = logger;
            this.config = config;
            this.clair = clair;
            this.cacheFactory = cacheFactory;
            Queue = queue;
        }

        private string GetKey(Image image) => $"clair:scans:{image.Digest}";

        /// <summary>
        /// Tries to get a summary of vulnerabilities from Clair. Returns null if the image has not been scanned yet.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="hard">Ignore any cached data and query Clair directly</param>
        /// <returns></returns>
        public Result GetScan(Image image, bool hard = false)
        {
            var cache = cacheFactory.Get<Result>();
            var key = GetKey(image);

            // if we have a cached scan, return it
            if (!hard && cache.TryGet(key, out var cachedResult))
            {
                return cachedResult;
            }
            // if we don't have a Clair instance handy, return null
            else if (clair == null)
            {
                return null;
            }
            // otherwise, try to get the latest scan result from Clair
            else
            {
                try
                {
                    var scanResult = clair.GetLayerResult(image.Layers.First().Digest).Result.ToScanResult();
                    cache.Set(key, scanResult);

                    return scanResult;
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerException is ApiException && ((ApiException)ex.InnerException).StatusCode == System.Net.HttpStatusCode.NotFound) { return null; }
                    else
                    {
                        throw;
                    }
                }
            }
        }
      
        /// <summary>
        /// Submits all layers of an image to Clair for scanning. If a layer has been scanned previously, it will be skipped.
        /// Repository info is necessary to download blobs for scanning, but once scanned from any repository blobs do not need to
        /// be rescanned/analyzed. Calling GetScan(hard: true) will always return the most current available vulnerability analysis.
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="repository"></param>
        /// <param name="image"></param>
        public void RequestScan(string repository, Image image, string host, string authorization)
        {
            var cache = cacheFactory.Get<Result>();
            var lockTime = new TimeSpan(0, 5, 0);

            // if this image is already in cache, skip it entirely
            if (!cache.Exists(GetKey(image)))
            {
                bool layerErrors = false;
                string lastError = string.Empty;
                using (var scanlock = cache.TakeLock($"scan:{image.Digest}", lockTime, lockTime))
                {
                    Layer previousLayer = null;
                    foreach (var layer in image.Layers.Reverse())
                    {
                        if (!CheckLayerScanned(layer))
                        {
                            var uri = new Uri(RegistryCredentials.HostToEndpoint(host, $"{repository}/blobs/{layer.Digest}"));

                            var request = new ClairLayerRequest
                            {
                                Layer = new ClairLayerRequest.LayerRequest
                                {
                                    Name = layer.Digest,
                                    ParentName = previousLayer?.Digest,
                                    Path = uri.ToString(),
                                    Headers = string.IsNullOrEmpty(authorization) ? null : new ClairLayerRequest.LayerRequest.LayerRequestHeaders { Authorization = authorization }
                                }
                            };

                            try
                            {
                                var tokenSource = new CancellationTokenSource();
                                tokenSource.CancelAfter(60000);
                                clair.SubmitLayer(request, tokenSource.Token).Wait();
                            }
                            catch (AggregateException ex)
                            {
                                if (ex.InnerException is ApiException && ((ApiException)ex.InnerException).StatusCode == (System.Net.HttpStatusCode)422)
                                {
                                    // at least one layer had issues, which may or may not have invalidated the scan
                                    // this can be transient, it can be a false error, or it can genuinely mean the image is currently unscannable
                                    // https://github.com/coreos/clair/issues/543
                                    layerErrors = true;
                                    var errorContent = (ex.InnerException as ApiException).Content;
                                    try
                                    {
                                        var json = JObject.Parse(errorContent);
                                        lastError = (string)json["Error"]["Message"];
                                    }
                                    catch { logger.LogError($"Could not parse Clair error response '{errorContent}'", ex); }
                                    continue;
                                }
                                else { throw; }
                            }
                        }
                        previousLayer = layer;
                    }

                    // if any layers failed above, check that we can get a valid result, and if not set an error entry so we can avoid further attempts for now
                    if (layerErrors && !CheckLayerScanned(image.Layers.First()))
                    {
                        cache.Set(GetKey(image), new Result
                        {
                            Status = RequestStatus.Failed,
                            Message = lastError ?? "At least one layer of the image could not be scanned."
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Checks to see if Clair has already scanned a given layer
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private bool CheckLayerScanned(Layer layer)
        {
            try
            {
                var result = clair.GetLayerResult(layer.Digest, vulnerabilities: false).Result;
                return true;
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is ApiException && ((ApiException)ex.InnerException).StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
