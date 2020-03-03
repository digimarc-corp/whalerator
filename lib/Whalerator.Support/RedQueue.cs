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
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Model;
using Whalerator.Queue;

namespace Whalerator.Support
{
    public class RedQueue<T> : IWorkQueue<T> where T : WorkItem
    {
        /*
         * Ideally this will get reimplemented with a full reliable-queue pattern eventually, but it seems low priority.
         * Failures on the queue will be resubmitted by the polling process, and otherwise we can just fail cheaply and move on
         */

        private IConnectionMultiplexer _Mux;
        private ILogger<RedQueue<T>> _Logger;
        private string _QueueName;

        public RedQueue(IConnectionMultiplexer redisMux, ILogger<RedQueue<T>> logger, string queueName)
        {
            _Mux = redisMux;
            _Logger = logger;
            _QueueName = queueName;
        }

        public bool Contains(T workItem) => Contains(workItem.WorkItemKey);

        public bool Contains(string key) => _Mux.GetDatabase().KeyExists(key);


        public T Pop()
        {
            var db = _Mux.GetDatabase();
            var key = db.ListRightPop(_QueueName);
            if (key.IsNullOrEmpty)
            {
                return null;
            }
            else
            {
                var json = db.StringGet(key.ToString());
                if (json.HasValue)
                {
                    db.KeyDelete(key.ToString());
                    return JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    _Logger.LogWarning($"Got bad workitem key: {key}");
                    return null;
                }
            }
        }

        public void Push(T workItem)
        {
            var db = _Mux.GetDatabase();
            // expiration is aggressive to allow queue failures to self-clear quickly
            db.StringSet(workItem.WorkItemKey, JsonConvert.SerializeObject(workItem), TimeSpan.FromSeconds(1200));
            db.ListLeftPush(_QueueName, workItem.WorkItemKey);
        }

        public bool TryPush(T workItem)
        {
            if (Contains(workItem)) { return false; }
            else
            {
                Push(workItem);
                return true;
            }
        }
    }
}
