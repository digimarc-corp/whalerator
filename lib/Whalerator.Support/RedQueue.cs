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

using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Model;

namespace Whalerator.Support
{
    public class RedQueue : IWorkQueue
    {
        private IConnectionMultiplexer _Mux;
        private int _Db;
        const string _Key = "workitems";

        public RedQueue(IConnectionMultiplexer redisMux, int db)
        {
            _Mux = redisMux;
            _Db = db;
        }

        public Whaleration Pop()
        {
            var json = _Mux.GetDatabase(_Db).ListRightPop(_Key);
            return json.IsNullOrEmpty ? null : JsonConvert.DeserializeObject<Whaleration>(json);
        }

        public void Push(Whaleration workItem) =>
            _Mux.GetDatabase(_Db).ListLeftPush(_Key, JsonConvert.SerializeObject(workItem));

    }
}
