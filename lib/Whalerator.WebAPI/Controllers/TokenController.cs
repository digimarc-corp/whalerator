using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Whalerator.Client;
using Whalerator.Support;
using Whalerator.WebAPI.Contracts;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/Token")]
    public class TokenController : Controller
    {
        private ICryptoAlgorithm _Crypto;

        public TokenController(ICryptoAlgorithm crypto)
        {
            _Crypto = crypto;
        }

        [HttpPost]
        public IActionResult Post([FromBody]RegistryCredentials credentials)
        {
            try
            {
                var handler = new AuthHandler();
                handler.Login(credentials.Registry, credentials.Username, credentials.Password);
                var json = JsonConvert.SerializeObject(credentials);
                var cipherText = _Crypto.Encrypt(json);

                var jwt = Jose.JWT.Encode(new Token { Crd = cipherText }, _Crypto.ToRSACryptoServiceProvider(), Jose.JwsAlgorithm.RS256);
                return Ok(new { token = jwt });
            }
            catch
            {
                return Unauthorized();
            }
        }
    }
}
