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

using Whalerator.DockerClient;
using Whalerator.Model;
using Whalerator.Queue;

namespace Whalerator.Security
{
    public interface ISecurityScanner
    {
        /// <summary>
        /// Fetch the results of a previously executed scan
        /// </summary>
        /// <param name="image"></param>
        /// <param name="hard"></param>
        /// <returns></returns>
        Result GetScan(Image image, bool hard = false);

        /// <summary>
        /// Submit a scan request to the scanning engine
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="repository"></param>
        /// <param name="image"></param>
        void RequestScan(string repository, Image image, string host, string authorization);

        IWorkQueue<ScanRequest> Queue { get; }
    }
}
