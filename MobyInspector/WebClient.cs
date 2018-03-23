using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector
{
    public class WebClient : IWebClient
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string Get(Uri uri)
        {
            throw new NotImplementedException();
        }
    }
}
