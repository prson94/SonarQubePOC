CREATE PROCEDURE [fusion].[FindEagleToDBRelationships]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	Declare @RelationshipList Table(StartID int,EndID Int);
	Declare @StartID int;
	Declare @EndID int;

	-- Eagle Inventory of Table to SQL Server DB Table
	insert into [fusion].[StagingRelationUnresolved]
		select 
			f.SOURCEID as 'StartID',
			f2.SOURCEID as 'EndID',
			CURRENT_TIMESTAMP
		from 
			fusionattribute f
			inner join fusionattribute f2 on ( f.name = f2.name and f.fusionattributetypeid = 2 and f2.fusionattributetypeid = 204)
			inner join fusionattribute fparent on (f.parentid = fparent.id)
			inner join fusionattribute f2parent on (f2.parentid = f2parent.id)
		where 
			f2parent.sourceid + '.DBO' = fparent.sourceid
			and not exists
				(select 
					1
				from
					[intersectnode] si 
					inner join fusionattribute sfa on (si.objectid = sfa.id and si.objecttype = 'fusionattribute' and sfa.fusionattributetypeid = 2)
					inner join [intersectnode] si2 on (si.intersectid = si2.intersectid and si2.id != si.id) 
					inner join fusionattribute sfa2 on (sfa2.id = si2.objectid and si2.objecttype = 'FusionAttribute' and sfa2.fusionattributetypeid = 204) 
				where 
					sfa.sourceid = f.sourceid and sfa2.sourceid = F2.sourceid)
			and not exists
				(	select 
						1
					from
						fusion.stagingrelationunresolved sru
					where
						sru.startid = f.sourceid and sru.endid = f2.sourceid
				)

	-- Eagle Field Attribute to SQL Server DB Column field attribute type = 201, sql server column type = 3
	insert into [fusion].[StagingRelationUnresolved]
		select 
			fa.sourceid as 'StartID',
			faSQLCol.sourceid as 'EndID',
			CURRENT_TIMESTAMP
		from
			fusionattribute fa
			inner join intersectnode i on (i.objectid = fa.id and i.objecttype = 'fusionattribute')
			inner join intersectnode i2 on (i.intersectid = i2.intersectid and i2.id != i.id)
			inner join fusionattribute fa2 on (fa2.id = i2.objectid and i2.objecttype = 'FusionAttribute') -- the inventory of field
			inner join fusionattribute faTbl on (fa2.parentid = faTbl.id) -- the table
			inner join fusionattribute faDB on (faTbl.parentid = faDB.id) -- the db
			inner join fusionattribute faSQLCol on (faSQLCol.Name = fa2.Name and faSQLCol.fusionattributetypeid = 3)
			inner join fusionattribute faSQLTbl on (faSQLCol.ParentID = faSQLTbl.ID and faSQLTbl.Name = faTbl.Name)
			inner join fusionattribute faSQLSchema on (faSQLTbl.ParentID = faSQLSchema.ID and faSQLSchema.SourceID  = faDB.sourceid +'.DBO' )--and faSQLDb.Name = faDB.Name)	
		where
			fa.fusionattributetypeid = 201
			and 
			fa2.fusionattributetypeid = 205
			and not exists
				(select 
					1
				from
					[intersectnode] si 
					inner join fusionattribute sfa on (si.objectid = sfa.id and si.objecttype = 'fusionattribute' and sfa.fusionattributetypeid = 201)
					inner join [intersectnode] si2 on (si.intersectid = si2.intersectid and si2.id != si.id) 
					inner join fusionattribute sfa2 on (sfa2.id = si2.objectid and si2.objecttype = 'FusionAttribute' and sfa2.fusionattributetypeid = 3) 
				where 
					sfa.sourceid = fa.sourceid and sfa2.sourceid = faSQLCol.sourceid)
			and not exists
				(	select 
						1
					from
						fusion.stagingrelationunresolved sru
					where
						sru.startid = fa.sourceid and sru.endid = faSQLCol.sourceid
				)

end
