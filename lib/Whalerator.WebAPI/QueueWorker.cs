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
using Whalerator.Scanner;

namespace Whalerator.WebAPI
{
    public class QueueWorker : IHostedService
    {
        private ILogger<QueueWorker> _Logger;
        private ConfigRoot _Config;
        private IWorkQueue _Queue;
        private ISecurityScanner _Scanner;
        private IRegistryFactory _RegistryFactory;
        private RegistryAuthenticationDecoder _AuthDecoder;
        private Timer _Timer;

        public QueueWorker(ILogger<QueueWorker> logger, ConfigRoot config, IWorkQueue queue, ISecurityScanner scanner, IRegistryFactory regFactory,
            RegistryAuthenticationDecoder decoder)
        {
            _Logger = logger;
            _Config = config;
            _Queue = queue;
            _Scanner = scanner;
            _RegistryFactory = regFactory;
            _AuthDecoder = decoder;
        }

        void StartTimer() => _Timer.Change(5000, 5000);
        void PauseTimer() => _Timer.Change(Timeout.Infinite, 0);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _Logger.LogInformation("QueueWorker starting");

            _Timer = new Timer(DoWork);
            StartTimer();

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            try
            {
                PauseTimer();
                var workItem = _Queue.Pop();
                while (workItem != null)
                {
                    switch (workItem.Action)
                    {
                        case WhalerationType.SecurityScan:
                            DoScan(workItem);
                            break;
                        default:
                            throw new NotImplementedException($"Don't know how to do a {workItem.Action.ToString()}!");
                    }

                    workItem = _Queue.Pop();
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "An error occurred while processing a queued work item.");
            }
            StartTimer();
        }

        private void DoScan(Whaleration item)
        {
            try
            {
                var auth = _AuthDecoder.AuthenticateAsync(item.Authorization).Result;
                if (!auth.Succeeded)
                {
                    _Logger.LogError(auth.Failure, "Authorization failed for the work item. A token may have expired since it was first submitted.");
                }
                else
                {
                    var registry = _RegistryFactory.GetRegistry(auth.Principal.ToRegistryCredentials());

                    var image = registry.GetImages(item.TargetRepo, item.TargetDigest, true);
                    if (image.Count() != 1) { throw new Exception($"Couldn't find a valid image for {item.TargetRepo}:{item.TargetDigest}"); }

                    _Scanner.RequestScan(registry, item.TargetRepo, image.First());
                    _Logger.LogInformation($"Completed submitting {item.TargetRepo}:{item.TargetDigest} to {_Scanner.GetType().Name} for analysis.");
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, $"Processing failed for work item\n {Newtonsoft.Json.JsonConvert.SerializeObject(item)}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
