using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Whalerator
{
    public static class ExtensionMethods
    {
        public static TValue Get<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key) where TValue : class
        {
            return dict.ContainsKey(key) ? dict[key] : null;
        }

        // Compares string values with flag to control case sensitivity.
        // Overloading Equals(this string, etc) does not work because of ambiguity with object.Equals(...)
        public static bool Matches(this string str, string value, bool ignoreCase)
        {
            return ignoreCase ? str.Equals(value, StringComparison.InvariantCultureIgnoreCase) : str == value;
        }

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
