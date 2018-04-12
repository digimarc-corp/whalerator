using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Client
{
    public class Authorization
    {
        public string JWT { get; set; }
        public string Realm { get; set; }
        public string Service { get; set; }

        public static string CacheKey(string registry, string username, string password, string scope)
        {
            var rawKey = $"{registry}:{username}:{password}:{scope}";
            var hash = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(rawKey));
            return Convert.ToBase64String(hash);
        }
    }
}
