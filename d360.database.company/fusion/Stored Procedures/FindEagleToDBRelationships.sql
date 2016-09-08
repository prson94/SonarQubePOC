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
		select	f.SOURCEID as 'StartID',
				f2.SOURCEID as 'EndID',
				CURRENT_TIMESTAMP
		from	fusionattribute f
				inner join fusionattribute f2 on ( f.name = f2.name and f.fusionattributetypeid = 2 and f2.fusionattributetypeid = 204)
				inner join fusionattribute fparent on (f.parentid = fparent.id)
				inner join fusionattribute f2parent on (f2.parentid = f2parent.id)
		where	f2parent.sourceid + '.DBO' = fparent.sourceid and 
				not exists	(
							select	1
							from	[Intersect] I
									inner join FusionAttribute sfa on I.Subject = 'FusionAttribute' and I.SubjectID = sfa.ID and sfa.FusionAttributeTypeID = 2 and sfa.SourceID = f.SourceID
									inner join FusionAttribute sfa2 on I.Object = 'FusionAttribute' and I.ObjectID = sfa2.ID and sfa2.FusionAttributeTypeID = 204 and sfa2.SourceID = F2.SourceID
							) and 
				not exists	(
							select	1
							from	fusion.stagingrelationunresolved sru
							where	sru.startid = f.sourceid and sru.endid = f2.sourceid
							)

	-- Eagle Field Attribute to SQL Server DB Column field attribute type = 201, sql server column type = 3
	insert into [fusion].[StagingRelationUnresolved]
		select	fa.sourceid as 'StartID',
				faSQLCol.sourceid as 'EndID',
				CURRENT_TIMESTAMP
		from	[Intersect] i
				inner join fusionattribute fa on I.Subject = 'FusionAttribute' and I.SubjectID = fa.ID
				inner join fusionattribute fa2 on I.Object = 'FusionAttribute' and I.ObjectID = fa2.ID -- the inventory of field
				inner join fusionattribute faTbl on (fa2.parentid = faTbl.id) -- the table
				inner join fusionattribute faDB on (faTbl.parentid = faDB.id) -- the db
				inner join fusionattribute faSQLCol on (faSQLCol.Name = fa2.Name and faSQLCol.fusionattributetypeid = 3)
				inner join fusionattribute faSQLTbl on (faSQLCol.ParentID = faSQLTbl.ID and faSQLTbl.Name = faTbl.Name)
				inner join fusionattribute faSQLSchema on (faSQLTbl.ParentID = faSQLSchema.ID and faSQLSchema.SourceID  = faDB.sourceid +'.DBO' )--and faSQLDb.Name = faDB.Name)	
		where	fa.fusionattributetypeid = 201	and 
				fa2.fusionattributetypeid = 205 and 
				not exists	(
							select	1
							from	[Intersect] I
									inner join FusionAttribute sfa on I.Subject = 'FusionAttribute' and I.SubjectID = sfa.ID and sfa.FusionAttributeTypeID = 201 and sfa.SourceID = fa.SourceID
									inner join FusionAttribute sfa2 on I.Object = 'FusionAttribute' and I.ObjectID = sfa2.ID and sfa2.FusionAttributeTypeID = 3 and sfa2.SourceID = faSQLCol.SourceID
							) and 
				not exists	(
							select	1 
							from	fusion.stagingrelationunresolved sru
							where	sru.startid = fa.sourceid and sru.endid = faSQLCol.sourceid
							)

end
GO

