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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Whalerator.Test
{
    public class Intervals
    {
        [Theory, ClassData(typeof(TimeSpanData))]
        public void CanParseIntervals(string str, TimeSpan? expected)
        {
            if (str == null)
            {
                Assert.Throws<ArgumentNullException>(() => IntervalParser.Parse(str));
            }
            else if (expected == null)
            {
                Assert.Throws<ArgumentException>(() => IntervalParser.Parse(str));
            }
            else
            {
                Assert.Equal(expected, IntervalParser.Parse(str));
            }
        }

        [Theory, ClassData(typeof(TimeSpanData))]
        public void CanTryParseIntervals(string str, TimeSpan? expected)
        {
            Assert.Equal(expected == null ? false : true, IntervalParser.TryParse(str, out var timeSpan));
            Assert.Equal(expected ?? default, timeSpan);
        }

        public class TimeSpanData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "10s", TimeSpan.FromSeconds(10) };
                yield return new object[] { "10m", TimeSpan.FromMinutes(10) };
                yield return new object[] { "24h", TimeSpan.FromDays(1) };
                yield return new object[] { "1y", TimeSpan.FromDays(365) };
                yield return new object[] { "2.5m", TimeSpan.FromSeconds(150) };
                yield return new object[] { "10ms", TimeSpan.FromMilliseconds(10) };
                yield return new object[] { "0", TimeSpan.FromSeconds(0) };
                yield return new object[] { "0m", TimeSpan.FromSeconds(0) };
                yield return new object[] { "", null };
                yield return new object[] { null, null };
                yield return new object[] { "-42h", null };
                yield return new object[] { "1x", null };
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
