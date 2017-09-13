using d360.core.entities;
using System.Data.Entity;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetType> AssetTypes { get; set; }

        #endregion

        #region Engine Methods


        #endregion
    }
}
