using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Client
{
    public class DistributionFactory : IDistributionFactory
    {
        public IDistributionClient GetClient(string host, IAuthHandler handler) => new DistributionClient(handler) { Host = host };
    }
}
