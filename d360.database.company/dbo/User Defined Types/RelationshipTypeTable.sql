CREATE TYPE [dbo].[RelationshipTypeTable] AS TABLE(
	ID int identity,
	startpromotedobjecttype varchar(25),
	startpromotedobjecttypeid int, 
	endpromotedobjecttype varchar(25),
	endpromotedobjecttypeid int
)

