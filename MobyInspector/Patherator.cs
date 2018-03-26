using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MobyInspector
{
    public class Patherator : IPatherator
    {
        /// <summary>
        /// Takes a search path within a tar'd AUFS diff, and returns a normalized path and whiteout strings
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public (string searchPath, string fileWhiteout, string pathWhiteout) Parse(string search)
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
