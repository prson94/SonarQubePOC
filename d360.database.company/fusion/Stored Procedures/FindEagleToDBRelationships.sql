ALTER PROCEDURE [Fusion].[FindEagleToDBRelationships]
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

	
end


