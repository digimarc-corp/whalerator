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
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Whalerator
{
    public class SubStream : Stream
    {
        private readonly long offset;
        private readonly long length;

        public SubStream(Stream stream, long length)
        {
            InnerStream = stream;
            this.offset = stream.Position;
            this.length = length;
        }

        public SubStream(Stream stream, long offset, long length)
        {
            InnerStream = stream;
            this.offset = offset;
            this.length = length;
        }

        public override bool CanRead => InnerStream.CanRead;

        public override bool CanSeek => InnerStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => length;

        public override long Position { get => InnerStream.Position - offset; set => InnerStream.Position = value + offset; }
        public Stream InnerStream { get; }
        public bool OwnInnerStream { get; set; }

        public override void Flush() => InnerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var innerCount = (int)Math.Min(count, this.length - Position);
            return InnerStream.Read(buffer, offset, innerCount);
        }

        public override long Seek(long offset, SeekOrigin origin) => InnerStream.Seek(offset + this.offset, origin);

        public override void SetLength(long value) => throw new ReadOnlyException();

        public override void Write(byte[] buffer, int offset, int count) => throw new ReadOnlyException();

        public override ValueTask DisposeAsync()
        {
            if (OwnInnerStream) { InnerStream.Dispose(); }
            return base.DisposeAsync();
        }
    }
}
