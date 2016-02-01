CREATE TYPE [utility].[DiagramRelationshipTable] AS TABLE(
	ItemID int identity,
	IntersectTypeID int, 
	IntersectID int, 
	ID int, 
	SourceObject varchar(50),
	SourceObjectID int, 
	SourceIntersectTypeNodeID int, 
	[TargetObject] varchar(50), 
	TargetObjectID int, 
	TargetIntersectTypeNodeID int,
	[type] int, 
	predicateid int, 
	needsMapRecord int	
)