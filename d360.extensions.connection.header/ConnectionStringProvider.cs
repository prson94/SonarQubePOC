using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using d360.services.interfaces;

namespace d360.extensions.connection.header
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
            var cs = "";
            Guid companyID;
            
            string authorizationHeader = HttpContext.Current.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                var companySegment = authorizationHeader.Split(';')[0];

                if (Guid.TryParse(companySegment, out companyID))
                {
                    cs = Community.GetCompanyConnectionStringByID(companyID);
                }
            }

            return cs;
        }


        public Guid GetCompanyID()
        {
            Guid companyID = Guid.Empty;

            string authorizationHeader = HttpContext.Current.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                var companySegment = authorizationHeader.Split(';')[0];
                companyID = Guid.Parse(companySegment);
            }

            return companyID;
        }
    }
}
