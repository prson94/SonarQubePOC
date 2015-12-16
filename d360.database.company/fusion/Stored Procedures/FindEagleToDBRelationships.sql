create PROCEDURE [Fusion].[FindEagleToDBRelationships]
as
begin
	set NOCOUNT, ANSI_PADDING ON;
	SET ANSI_WARNINGS ON;

	Declare @RelationshipList Table(StartID int,EndID Int);
	Declare @StartID int;
	Declare @EndID int;

	-- Eagle Inventory of Table to SQL Server DB Table
	insert into @RelationshipList
		select 
			f.ID as 'StartID',
			f2.ID as 'EndID'
		from 
			fusionattribute f
			inner join fusionattribute f2 on ( f.name = f2.name and f.fusionattributetypeid = 2 and f2.fusionattributetypeid = 204)
			inner join fusionattribute fparent on (f.parentid = fparent.id)
			inner join fusionattribute f2parent on (f2.parentid = f2parent.id)
		where 
			f2parent.sourceid + '.DBO' = fparent.sourceid

	--TODO need to convert to set based operations!!!	
	--commented out pending mikes changes and changes to relationships
	/*WHILE EXISTS(SELECT * FROM @RelationshipList)
	begin
		Select Top 1 @StartID = StartID, @EndID = EndID From @RelationshipList;

		exec [dbo].[AddRelationship] @ResourceID = 1, getdate(), 'FusionAttribute', @StartID, 2, NULL, NULL, 'FusionAttribute', @EndID

		Delete @RelationshipList Where StartID = @StartID and EndID = @EndID;
	end;*/

	-- Eagle Inventory of Field to SQL Server DB Column
	
end


