using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace igx.UnitTests.Core
{
    //This class is used to store the common SQL queries used.
    public static class SqlConstants
    {
        #region AssetControllerTestSQL
        public const string SQL_FOR_GETASSETTYPESASYNC = @"
                                                            SELECT      A.[Name]
                                                                        ,A.[Description]
                                                                        ,A.[Class] as ClassID
                                                                        ,A.[Notes]
                                                                        ,A.[uid],
                                                                        P.[Path]
                                                            FROM        AssetType A
                                                                        cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                                                            where       A.[State] = 1
                                                            order by    P.[Path]
                                                            ";

        #endregion
    }
}
