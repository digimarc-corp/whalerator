using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private ICache<Authorization> _Cache;

        public ILogger<TokenController> Logger { get; }

        public TokenController(ICryptoAlgorithm crypto, ICache<Authorization> cache, ILogger<TokenController> logger)
        {
            _Crypto = crypto;
            _Cache = cache;
            Logger = logger;
        }

        [HttpPost]
        public IActionResult Post([FromBody]RegistryCredentials credentials)
        {
            try
            {
                var handler = new AuthHandler(_Cache);
                handler.Login(credentials.Registry, credentials.Username, credentials.Password);
                var json = JsonConvert.SerializeObject(credentials);
                var cipherText = _Crypto.Encrypt(json);

                var jwt = Jose.JWT.Encode(new Token { Crd = cipherText }, _Crypto.ToDotNetRSA(), Jose.JwsAlgorithm.RS256);
                return Ok(new { token = jwt });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error authenticating token request.");
                return Unauthorized();
            }
        }
    }
}
