using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using d360.services.interfaces;

namespace d360.extensions.connection.url
{
    public class ConnectionStringProvider: IConnectionStringProvider
    {
        private ICommunityService Community;

        public ConnectionStringProvider(ICommunityService community)
        {
            Community = community;
        }

        public string GetConnectionString()
        {
            return Community.GetCompanyConnectionStringByUri(HttpContext.Current.Request.Url);
        }

        public Guid GetCompanyID()
        {
            return Community.GetCompanyIDByUri(HttpContext.Current.Request.Url);
        }
    }
}
