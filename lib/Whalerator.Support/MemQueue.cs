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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Whalerator.Model;
using Whalerator.Queue;

namespace Whalerator.Support
{
    public class MemQueue<T> : IWorkQueue<T> where T : WorkItem
    {
        private ConcurrentQueue<T> queue;

        public MemQueue()
        {
            queue = new ConcurrentQueue<T>();
        }

        public bool Contains(T workItem) => Contains(workItem.WorkItemKey);

        public bool Contains(string key) => queue.Any(i => i.WorkItemKey.Equals(key));

        public T Pop()
        {
            if (queue.TryDequeue(out var item)) { return item; }
            else { return null; }
        }

        public void Push(T workItem) => queue.Enqueue(workItem);

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
