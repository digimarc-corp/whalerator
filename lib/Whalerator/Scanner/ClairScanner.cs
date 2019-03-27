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
using Whalerator.Client;
using Whalerator.Config;
using Whalerator.Model;
using Whalerator.Scanner;

namespace Whalerator.Scanner
{
    public class ClairScanner : ISecurityScanner
    {
        private ILogger _Log;
        private ConfigRoot _Config;

        public ClairScanner(ILogger logger, ConfigRoot config)
        {
            _Log = logger;
            _Config = config;
        }

        public ScanResult GetScan(Image image)
        {
            try
            {
                var api = Refit.RestService.For<IClairAPI>(_Config.Clair.ClairApi);
                var result = api.GetLayerResult(image.Layers.First().Digest).Result;

                return ScanResult.FromClair(result);
            }
            catch (AggregateException ex)
            {
                if (ex.InnerException is ApiException && ((ApiException)ex.InnerException).StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }
        }

        public void RequestScan(IRegistry registry, string repository, string digest)
        {
            _Log?.LogInformation($"Scan requested for {repository}:{digest}");
        }
    }
}
