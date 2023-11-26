using d360.extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web
{
	public class BaseMiddleware
    {
	    protected Func<IDictionary<string, object>, Task> Next { get; }

	    internal ICachingProvider Cache;
		internal ILogger Log;

        public BaseMiddleware(Func<IDictionary<string, object>, Task> next)
        {
	        Next = next;
			Cache = DependencyResolver.Current.GetService<ICachingProvider>();
			Log = DependencyResolver.Current.GetService<ILogger>();
        }
    }
}
