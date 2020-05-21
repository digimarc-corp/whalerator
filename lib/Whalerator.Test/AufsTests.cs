using System;
using System.Collections;
using System.Collections.Generic;
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

using System.Linq;
using System.Text;
using Whalerator.Content;
using Xunit;

namespace Whalerator.Test
{
    public class AufsTests
    {
        [Theory]
        [InlineData("/test", false, "/test")]
        [InlineData("/.wh.test", true, "/test")]
        [InlineData("/usr/.wh.test", true, "/usr/test")]
        [InlineData("/test/.wh..wh.opq", true, "/test")]
        [InlineData("/.wh..wh.opq", true, "")]
        [InlineData("/foo/.wh./bar", false, "/foo/.wh./bar")]
        [InlineData(null, false, null)]
        [InlineData("", false, "")]
        public void RecognizesWhiteouts(string entry, bool isWhiteout, string target)
        {
            var indexer = new AufsFilter();
            Assert.Equal(isWhiteout, indexer.IsWhiteout(entry, out var t));
            Assert.Equal(target, t);
        }

        [Theory]
        [InlineData("/test", "/test", true)]
        [InlineData("/test", "/test.file", false)]
        [InlineData("/test", "/test/file", true)]
        [InlineData("/test", "/test2/file", false)]
        [InlineData("/test", "/tester", false)]
        [InlineData("/test", "/joe", false)]
        [InlineData("", "/joe", true)]
        public void AppliesWhiteouts(string whiteoutTarget, string path, bool isHidden)
        {
            var indexer = new AufsFilter();
            Assert.Equal(isHidden, indexer.MatchesAnyWh(path, new[] { whiteoutTarget }));
        }

        [Theory]
        [ClassData(typeof(FlatIndexData))]
        public void CanCoalesceIndexes(IEnumerable<string> flatFiles, IEnumerable<string> finalFiles, string target)
        {
            var indexer = new AufsFilter();
            var flatIndexes = flatFiles
                .Select(f => f.Split(','))
                .Select(s => (s[0], int.Parse(s[1]), s[2]))
                .GroupBy(t => t.Item1)
                .Select(g => new LayerIndex { Digest = g.Key, Depth = g.Min(i => i.Item2), Files = g.Select(i => i.Item3) });

            var processed = indexer.FilterLayers(flatIndexes, target)
                .SelectMany(l => l.Files.Select(f => $"{l.Digest},{l.Depth},{f}"));

            Assert.Equal(finalFiles, processed);
        }

        public class FlatIndexData : IEnumerable<object[]>
        {
            static string[] Simple = new[] { "a,1,/test" };
            static string[] SimpleWhiteout = new[] { "a,1,/test", "a,1,/.wh.foo", "b,2,/foo" };
            static string[] SimpleOpq = new[] { "a,1,/test", "a,1,/.wh..wh.opq", "b,2,/foo" };
            static string[] SimpleCoalesed = new[] { "a,1,/test" };

            static string[] Moderate = new[] { "a,1,/test/foo", "b,2,/test/.wh..wh.opq", "c,4,/bar", "c,4,/test/baz" };
            static string[] ModerateCoalesced = new[] { "a,1,/test/foo", "c,4,/bar" };
            static string[] ModerateTargeted = new[] { "a,1,/test/foo" };


            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { Simple, SimpleCoalesed, null };
                yield return new object[] { SimpleWhiteout, SimpleCoalesed, null };
                yield return new object[] { SimpleOpq, SimpleCoalesed, null };
                yield return new object[] { Moderate, ModerateCoalesced, null };
                yield return new object[] { Moderate, ModerateTargeted, "/test/foo" };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
