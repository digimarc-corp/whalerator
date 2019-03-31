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
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Model;
using Whalerator.Scanner;

namespace Whalerator.Support
{
    public class ClairScanner : ISecurityScanner
    {
        private ILogger _Log;
        private ConfigRoot _Config;
        private IClairAPI _Clair;
        private ICacheFactory _CacheFactory;

        public ClairScanner(ILogger<ClairScanner> logger, ConfigRoot config, IClairAPI clair, ICacheFactory cacheFactory)
        {
            _Log = logger;
            _Config = config;
            _Clair = clair;
            _CacheFactory = cacheFactory;
        }

        private string GetKey(Image image) => $"volatile:scans:{image.Digest}";

        /// <summary>
        /// Trys to get a summary of vulnerabilities from Clair. Returns null if the image has not been scanned yet.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="hard">Ignore any cached data and query Clair directly</param>
        /// <returns></returns>
        public ScanResult GetScan(Image image, bool hard = false)
        {
            var cache = _CacheFactory.Get<ScanResult>();
            var key = GetKey(image);

            if (!hard && cache.TryGet(key, out var cachedResult))
            {
                return cachedResult;
            }
            else
            {
                try
                {
                    var scanResult = _Clair.GetLayerResult(image.Layers.First().Digest).Result.ToScanResult();
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
        public void RequestScan(IRegistry registry, string repository, Image image)
        {
            var cache = _CacheFactory.Get<ScanResult>();
            var lockTime = new TimeSpan(0, 5, 0);

            using (var scanlock = cache.TakeLock($"scan:{image.Digest}", lockTime, lockTime))
            {
                Layer previousLayer = null;
                foreach (var layer in image.Layers.Reverse())
                {
                    if (!CheckLayerScanned(layer))
                    {
                        var proxy = registry.GetLayerProxyInfo(repository, layer);
                        var request = new ClairLayerRequest
                        {
                            Layer = new ClairLayerRequest.LayerRequest
                            {
                                Name = layer.Digest,
                                ParentName = previousLayer?.Digest,
                                Headers = new ClairLayerRequest.LayerRequest.LayerRequestHeaders
                                {
                                    Authorization = proxy.LayerAuthorization
                                },
                                Path = proxy.LayerUrl
                            }
                        };

                        try
                        {
                            var tokenSource = new CancellationTokenSource();
                            tokenSource.CancelAfter(60000);
                            _Clair.SubmitLayer(request, tokenSource.Token).Wait();
                        }
                        catch (AggregateException ex)
                        {
                            // if this image is unscannable, set a cache entry so we avoid retrying it for a while. A future Clair update
                            // may be able to handle it, so use standard volatile expiration logic
                            if (ex.InnerException is ApiException && ((ApiException)ex.InnerException).StatusCode == (System.Net.HttpStatusCode)422)
                            {
                                continue;
                                // Clair seems to be having problems scanning some very standard images, so ignore 422's for now
                                // https://github.com/coreos/clair/issues/543
                                cache.Set(GetKey(image), new ScanResult
                                {
                                    Status = ScanStatus.Failed,
                                    Message = "Image cannot be scanned - it uses an unsupported OS or image format."
                                });
                            }
                            else { throw; }
                        }
                    }
                    previousLayer = layer;
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
                var result = _Clair.GetLayerResult(layer.Digest, vulnerabilities: false).Result;
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
