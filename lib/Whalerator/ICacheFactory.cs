using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator
{
    public interface ICacheFactory
    {
        ICache<T> Get<T>() where T : class;
    }
}
