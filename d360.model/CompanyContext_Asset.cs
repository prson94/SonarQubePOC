using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace d360.model
{
    partial class CompanyContext: BaseContext
    {
        #region DbSets

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetType> AssetTypes { get; set; }

        public DbSet<AssetApiModel> AssetApiModels { get; set; }

        public DbSet<FieldApiModel> FieldApiModels { get; set; }

        #endregion

        #region Engine Methods

        public List<GenerateAssetTypeSqlModel> GenerateAssetTypeSql(SystemObjects type, int typeID, PredicateType predicateType, out string sql, bool showPassword = false)
        {
            sql = @"
	select	A.ID as AssetID,
			A.ObjectID as ID,
			P.ParentID,
			A.AssetTypeID,
            {0}
			T.ObjectID as TypeID
	from	Asset A
			inner join AssetType T on T.ID = A.AssetTypeID and T.Object = @type and T.ObjectID = @id
            {1}
			outer apply (
						select	I.SubjectID as ParentID
						from	[Intersect] I
								inner join IntersectType IT on IT.ID = I.IntersectTypeID 
								inner join [Predicate] P on P.ID = IT.PredicateID and P.Type = @pt
						where	I.Object = A.Object and I.ObjectID = A.ObjectID
						) P
    {2}";
            var statements = Database.Connection.Query<GenerateAssetTypeSqlModel>(@"exec GenerateAssetTypeSql @type, @id, @pt, @showPassword", new { type = type.ToString(), id = typeID, pt = (int)predicateType, showPassword }).AsList<GenerateAssetTypeSqlModel>();

            var joins = string.Join(" ", statements.Where(i => !(!i.IsListable && i.SortOrder != 0)).Select(i => i.JoinStatement));
            var columns = string.Join(", ", statements.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).Select(i => i.ColumnStatement));
            if (!string.IsNullOrEmpty(columns)) columns += ", ";
            var sorts = string.Join(", ", statements.Where(i => !(!i.IsListable && i.SortOrder != 0)).OrderBy(i => i.SortOrder).Select(i => i.SortStatement));
            if (!string.IsNullOrEmpty(sorts)) sorts = "order by " + sorts;
            sql = string.Format(sql, columns, joins, sorts);

            return statements;
        }

        #endregion
    }
}
