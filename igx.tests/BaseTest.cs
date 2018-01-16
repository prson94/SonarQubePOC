using d360.core;
using d360.core.entities;
using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.tests
{
    public class BaseTest
    {
        internal List<int> getCompanies(bool developmentOnly = false)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var sql = "select ID from Company where ";
            if (developmentOnly)
            {
                sql += "DatabaseServerID = 6 and ";
            }
            sql += "Status = 'Active'";
            var list = cnn.Query<int>(sql).ToList();
            cnn.Close();
            cnn.Dispose();

            return list;
        }

        internal SqlConnection getCommunityConnection()
        {
            return new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
        }

        internal SqlConnection getCompanyConnection(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                cnn = new SqlConnection(
                    string.Format("server={0};Database=D3S_{1};User ID={2};Password={3}", db.Server, companyID, db.Username, db.Password)
                );
                db = null;
            }
            return cnn;
        }

        internal string getCompanyConnectionString(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                return $"server={db.Server};Database=D3S_{companyID};User ID={db.Username};Password={db.Password}";
            }
            else 
                return "";
        }

        internal static string getStaticCompanyConnectionString(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                return $"server={db.Server};Database=D3S_{companyID};User ID={db.Username};Password={db.Password}";
            }
            else
                return "";
        }
    }
}
