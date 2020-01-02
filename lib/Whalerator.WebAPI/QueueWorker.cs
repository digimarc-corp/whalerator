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
    public abstract class QueueWorker<T> : IHostedService where T : Whalerator.RequestBase
    {
        protected ILogger _Logger;
        protected ConfigRoot _Config;
        protected IWorkQueue<T> _Queue;
        protected IRegistryFactory _RegistryFactory;
        private Timer _Timer;

        protected QueueWorker(ILogger logger, ConfigRoot config, IWorkQueue<T> queue, IRegistryFactory regFactory)
        {
            _Logger = logger;
            _Config = config;
            _Queue = queue;
            _RegistryFactory = regFactory;
        }

        void StartTimer() => _Timer.Change(5000, 5000);
        void PauseTimer() => _Timer.Change(Timeout.Infinite, 0);

        public abstract void DoRequest(T request);

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
                    DoRequest(workItem);

                    workItem = _Queue.Pop();
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "An error occurred while processing a queued work item.");
            }
            StartTimer();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _Timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
