using System.Text;

namespace repositories.azure
{
	public abstract class Repository
	{
		public int CurrentUserId { get; set; }

		public Platform Platform { get { return Platform.Azure; } }

		public DapperConnectionProvider ConnectionProvider { get; set; }

		protected Repository(DapperConnectionProvider provider)
		{
			ConnectionProvider = provider;		
		}

		internal string CreateImportTemporaryTableSql(string type)
		{
			string token = "[[COLUMNS]]";
			string sql = $"create table #Items (ItemNumber int, [Uid] uniqueidentifier, IsValid bit, IsSuccess bit, {token} [Message] nvarchar(max));";
			string columns = "";
			switch (type)
			{
				case "Group":
					columns = "GroupID int, AssetID bigint, Name nvarchar(250), Description nvarchar(max), PrimaryOwnerUid uniqueidentifier, PrimaryOwnerResourceID int, SecondaryOwnerUid uniqueidentifier, SecondaryOwnerResourceID int, IsActiveDirectoryGroup bit, ";
					break;
				case "Resource":
					columns = "ResourceID int, AssetID bigint, Username nvarchar(500), Email nvarchar(500), FirstName nvarchar(250), LastName nvarchar(250), [State] int, IsAdministrator bit, ";
					break;
				default:
					// Nothing to do
					break;
			}

			sql = sql.Replace(token, columns);
			sql += "create table #Fields (ItemNumber int, FieldName nvarchar(250), FieldTypeID int, FieldValue nvarchar(max), LookupValue nvarchar(max));";

			return sql;
		}

		internal string CreateImportFieldValidationSql(string type, bool lookupsPassedByValue)
		{
			StringBuilder sql = new("");

			if (lookupsPassedByValue)
			{
				sql.AppendLine("update T set T.LookupValue = T.[FieldValue] from #Fields T inner join FieldType ST on ST.ID = T.FieldTypeID and ST.[Type] = 'Lookup';");
			}
			else
			{
				sql.Append(@"
declare @listFieldTypes table (FieldTypeID int, AllowMultipleValues bit);
declare @uniqueListValues table (FieldTypeID int, AllowMultipleValues bit, FieldValue nvarchar(max), LookupValue nvarchar(max))

insert into @listFieldTypes
	select	t.FieldTypeID, s.AllowMultipleValues
	from	#Fields t
			inner join FieldType s on s.ID = t.FieldTypeID and s.[Type] = 'Lookup'
	group by t.FieldTypeID, s.AllowMultipleValues;

insert into @uniqueListValues
	select	t.FieldTypeID, s.AllowMultipleValues, t.FieldValue
	from	#Fields t
			inner join @listFieldTypes s on s.FieldTypeID = t.FieldTypeID
			cross apply string_split(t.FieldValue, ',') tmv
	group by t.FieldTypeID, s.AllowMultipleValues, t.FieldValue;

update	t
set		t.LookupValue = s.[Value]
from	@uniqueListValues t
		inner join FieldLookupValue s on s.FieldTypeID = t.FieldTypeID and s.[Text] = t.FieldValue;

update	t
set		t.LookupValue = s.LookupValue
from	#Fields t
		inner join @uniqueListValues s on s.FieldTypeID = t.FieldTypeID and s.AllowMultipleValues = 0;

update	t
set		t.LookupValue = ms.LookupValue
from	#Fields t
		inner join FieldType ft on ft.ID = t.FieldTypeID and ft.[Type] = 'Lookup' and ft.AllowMultipleValues = 1
		cross apply (
			select	string_agg(s.LookupValue, ',') as LookupValue
			from	@uniqueListValues s
			where	s.FieldTypeID = t.FieldTypeID
					and LookupValue in (select [value] from string_split(t.FieldValue, ','))
		) ms;");
			}

			string idColumnName = "AssetID";
			switch (type)
			{
				case "Group":
					idColumnName = "GroupID";
					break;
				case "Resource":
					idColumnName = "ResourceID";
					break;
				default:
					// Nothing to do
					break;
			}

			sql.AppendLine($@"
merge	Field as t
using	(
		select	i.{idColumnName},
				i.AssetID,
				f.*
		from	#Fields f
				inner join #Items i on i.ItemNumber = f.ItemNumber
		) as s
on		(t.ObjectType = '{type}' and t.ObjectID = s.{idColumnName} and t.FieldTypeID = s.FieldTypeID)
when	matched then
update	set
		t.Value = iif(s.LookupValue is null, null, s.LookupValue),
		t.FormattedValue = iif(s.LookupValue is null, s.FieldValue, null),
		t.UpdatedBy = @userId,
		t.UpdatedOn = @date
when	not matched by target then
insert	(AssetID, ObjectType, ObjectID, FieldTypeID, [Value], FormattedValue, UpdatedBy, UpdatedOn)
values	(s.AssetID, '{type}', s.{idColumnName}, s.FieldTypeID, iif(s.LookupValue is null, null, s.LookupValue), iif(s.LookupValue is null, s.FieldValue, null), @userId, @date);

update	F
set		F.FormattedValue = utility.GetFormattedFieldLookupValueWithMultiple(FT.Type, FT.LookupDisplayFormat, FT.LookupObjectType, FT.LookupObjectID, F.Value, FT.AllowMultipleValues)
from	Field F
		inner join #Items i on i.AssetID = f.AssetId 
		inner join #Fields t on t.ItemNumber = i.ItemNumber and t.FieldTypeID = F.FieldTypeID and F.[Value] is not null
		inner join FieldType FT on FT.ID = f.FieldTypeID and FT.Type = 'Lookup';
");

			return sql.ToString();
		}

		internal string CreateImportAssetTableMergeSql(string type)
		{
			StringBuilder sql = new("");

			string objectTypeName = "";
			string idColumnName = "AssetID";
			switch (type)
			{
				case "Group":
					objectTypeName = "GroupType";
					idColumnName = "GroupID";
					break;
				case "Resource":
					objectTypeName = "ResourceType";
					idColumnName = "ResourceID";
					break;
				default:
					// Nothing to do
					break;
			}

			sql.AppendLine($@"
declare @assetTypeId int;
select @assetTypeId = ID from AssetType where Object = '{objectTypeName}';

merge	dbo.Asset as T
using	(select * from #Items) as S
on		(T.Object = '{type}' and T.ObjectID = S.{idColumnName})
when	matched then
update  set
		T.UpdatedOn = @date,
		T.UpdatedBy = @userId
when	not matched by target then
insert	([uid], [AssetTypeID], [State], [Object], [ObjectID], [CreatedOn], [CreatedBy], [UpdatedOn], [UpdatedBy])
values	(S.Uid, @assetTypeId, 1, '{type}', S.{idColumnName}, @date, @userId, @date, @userId);

update	T
set		T.AssetID = A.ID
from	#Items T
		inner join dbo.Asset A on A.Object = '{type}' and A.ObjectID = T.{idColumnName};");

			return sql.ToString();
		}

		internal string CreateImportCompleteExecutionSql()
		{
			return @"
update	E 
set		E.[State] = 4,
		E.CompletedOn = @date,
		E.[Total] = iif(Tc.Cnt = 0, E.[Total], Tc.Cnt),
		E.Processed = iif(Pc.Cnt = 0, E.Processed, Pc.Cnt),
		E.[Error] = iif(Ec.Cnt = 0, E.[Error], Ec.Cnt)
from	api.Execution E
		cross apply ( select count(1) as Cnt from #Items where IsSuccess = 0  ) Ec
		cross apply ( select count(1) as Cnt from #Items where IsSuccess = 1 ) Pc
		cross apply ( select count(1) as Cnt from #Items ) Tc
where	E.Id = @executionId";
		}
	}
}
