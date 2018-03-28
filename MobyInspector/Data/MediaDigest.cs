using MobyInspector.Model;
using System;
using System.Collections.Generic;

namespace MobyInspector.Data
{
    public class MediaDigest
    {
        public string MediaType { get; set; }
        public int Size { get; set; }
        public string Digest { get; set; }
        public IEnumerable<string> Urls { get; set; }

        public Layer ToLayer()
        {
            switch (MediaType)
            {
                case "application/vnd.docker.image.rootfs.diff.tar.gzip":
                    return new Layer { Digest = Digest, Size = Size };
                case "application/vnd.docker.image.rootfs.foreign.diff.tar.gzip":
                    return new ForeignLayer { Digest = Digest, Size = Size, Urls = Urls };
                default:
                    throw new Exception($"Can't create a Layer object for mediatype '{MediaType}'");
            }
        }
    }
}