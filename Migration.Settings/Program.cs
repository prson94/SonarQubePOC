using d360.core;
using d360.utils.company;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Settings
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
            {
                if (cnn.State != System.Data.ConnectionState.Open)
                {
                    cnn.Open();
                }

                int currentCompanyId = 0;
                bool isNewCompany = false;
                var companySettings = cnn.Query<dynamic>(@"
select  C.CompanyID, s.ID, c.Value 
from    Setting s 
        inner join CompanySetting c on c.SettingID = s.ID and ( (c.Value <> s.DefaultValue) or (c.Value is null and s.DefaultValue is not null) or (c.Value is not null and s.DefaultValue is null) ) and c.SettingID not in (66,68,70)
        inner join Company e on e.ID = c.CompanyID and e.EnvironmentLevel in (0,1,2,3)
order by c.CompanyID").ToList();

                SqlConnection env = null;
                companySettings.ForEach(cs =>
                {
                    var setting = new
                    {
                        CompanyID = (int)cs.CompanyID,
                        ID = (int)cs.ID,
                        Value = (string)cs.Value,
                    };

                    if (currentCompanyId != setting.CompanyID)
                    {
                        if (env != null && env.State != System.Data.ConnectionState.Closed)
                        {
                            env.Close();
                            env.Dispose();
                        }

                        currentCompanyId = setting.CompanyID;
                        isNewCompany = true;

                        try
                        {
                            env = CompanyConnectionUtils.GetCompanyConnection(currentCompanyId);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.GetFullExceptionData(false));
                        }
                    }


                    try
                    {
                        if (env != null && env.State != System.Data.ConnectionState.Open)
                        {
                            env.Open();
                        }
                        env.Execute(@"
if exists(select 1 from [Setting] where ID = @ID) 
begin 
    update [Setting] set [Value] = @Value where ID = @ID 
end 
else 
begin 
    insert [Setting] values (@ID, @Value) 
end", setting);
                    }
                    catch (Exception ex)
                    {
                        if (isNewCompany) 
                        {
                            Console.WriteLine($"Company: {setting.CompanyID}. " + ex.GetFullExceptionData(false));
                            isNewCompany = false;
                        }
                    }
                });

                // Pause.
                Console.ReadLine();
            }
        }
    }
}
