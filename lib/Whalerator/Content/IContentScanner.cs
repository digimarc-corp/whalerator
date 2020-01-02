using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Model;
using Whalerator.Queue;

namespace Whalerator.Content
{
    public interface IContentScanner
    {
        /// <summary>
        /// Fetch the results of a previously executed content scan
        /// </summary>
        /// <param name="image"></param>
        /// <param name="hard"></param>
        /// <returns></returns>
        Result GetPath(Image image, string path);

        /// <summary>
        /// Extract and index content at the given location
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="repository"></param>
        /// <param name="image"></param>
        void Index(IRegistry registry, string repository, Image image, string path);

        IWorkQueue<Request> Queue { get; }
    }
}
