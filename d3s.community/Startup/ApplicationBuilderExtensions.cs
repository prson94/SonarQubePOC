using Microsoft.AspNet.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace d3s.community.startup
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static IApplicationBuilder UseCompanyCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CompanyCheckMiddleware>();
        }

        public static IApplicationBuilder UseUserCheck(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserCheckMiddleware>();
        }
    }
}
