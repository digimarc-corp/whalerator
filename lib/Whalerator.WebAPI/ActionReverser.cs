using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public static class ActionReverser
    {
        /// <summary>
        /// Reformats a request containing a path of indeterminate length by moving a trailing action to the beginning. 
        /// Ex, /api/contoller/path/to/something/action could be reformatted as /api/controller/action/path/to/something.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseActionReverser(this IApplicationBuilder app, string root, int actions)
        {
            return app.Use(async (context, next) =>
            {
                if (!root.StartsWith("/")) { throw new ArgumentException("Root must be begin with '/'"); }

                var rootPath = new PathString(root);
                if (context.Request.Path.StartsWithSegments(rootPath))
                {
                    var rootLength = root.Trim('/').Split('/').Count();
                    var segments = context.Request.Path.Value.Trim('/').Split('/');
                    if (segments.Length >= rootLength + 2)
                    {
                        var repository = segments.Skip(rootLength).Take(segments.Length - (rootLength + actions));
                        var actionList = segments.Skip(segments.Length - actions).Take(actions);
                        context.Request.Path = new PathString($"{root}/{string.Join('/', actionList)}/{string.Join('/', repository)}");
                    }
                }

                await next.Invoke();
            });
        }
    }
}
