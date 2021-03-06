﻿/*
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


using System.Collections.Generic;
using Whalerator.Model;

namespace Whalerator.Content
{
    public interface IAufsFilter
    {
        IAsyncEnumerable<LayerIndex> FilterLayers(IAsyncEnumerable<LayerIndex> layers, params string[] targets);
        bool IsWhiteout(string path, out string target);
        bool MatchesAnyWh(string path, IEnumerable<string> wh);
    }
}