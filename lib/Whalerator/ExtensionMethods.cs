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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Whalerator.Client;
using Whalerator.Model;

namespace Whalerator
{
    public static class ExtensionMethods
    {
        /*
        public static Permissions GetPermissions(this IAuthHandler handler, string repository)
        {
            return handler.Authorize($"repository:{repository}:*") ? Permissions.Admin :
                   handler.Authorize($"repository:{repository}:push") ? Permissions.Push :
                   handler.Authorize($"repository:{repository}:pull") ? Permissions.Pull :
                   Permissions.None;
        }
        */

        public static bool IsDigest(this string str) => str.StartsWith("sha256:");

        public static string ToDigestPath(this string digest)
        {
            var parts = digest?.Split(':');
            if (parts == null || parts.Length != 2) { throw new FormatException("Could not parse as a digest string"); }
            else
            {
                return Path.Combine(parts[0], parts[1][..2], parts[1]);
            }
        }
        /*
        public static string ToImageSetDigest(this IEnumerable<Model.Image> images)
        {
            return string.Join(":", images.Select(i => i.Digest));
        }
        */
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
        {
            return dict.ContainsKey(key) ? dict[key] : null;
        }
        /*
        // Compares string values with flag to control case sensitivity.
        // Overloading Equals(this string, etc) does not work because of ambiguity with object.Equals(...)
        public static bool Matches(this string str, string value, bool ignoreCase)
        {
            return ignoreCase ? str.Equals(value, StringComparison.InvariantCultureIgnoreCase) : str == value;
        }
        */
        /// <summary>
        /// Takes a search path within a tar'd AUFS diff, and return a normalized path and whiteout strings
        /// </summary>
        /// <param name="search">Search target</param>
        /// <returns>Tuple with search path, file whiteout, and path whiteout/opaque block</returns>
        public static (string searchPath, string fileWhiteout, string pathWhiteout) Patherate(this string search)
        {
            search = search.Replace('\\', '/').TrimStart('/');
            var folder = Path.GetDirectoryName(search).Replace('\\', '/').TrimStart('/');
            var file = Path.GetFileName(search);

            var fileWhiteout = string.IsNullOrEmpty(folder) ? $".wh.{file}" : $"{folder}/.wh.{file}";
            var opqWhiteout = string.IsNullOrEmpty(folder) ? ".wh..wh.opq" : $"{folder}/.wh..wh.opq";

            return (search, fileWhiteout, opqWhiteout);
        }
    }
}
