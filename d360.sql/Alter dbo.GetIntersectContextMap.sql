[GetIntersectContextMap] 4, 214619, 1
ALTER PROCEDURE [dbo].[GetIntersectContextMap]
(
--declare
	@CompanyID int,
	@IntersectID int,
	@TypeIDToRemove int
--set @CompanyID = 4
--set @IntersectID = 214619--214614--214637--214615--214455
--set @TypeIDToRemove = 1
)
AS
BEGIN
	SET NOCOUNT ON;

	declare @type varchar(250);
	set @type = 'Intersect';
	--declare @tbl table (ID int, ParentID int, IntersectID int);

	--with cte as 
	--( 
	--select	C.ID,
	--		C.SourceIntersectContextID as ParentID,
	--		C.IntersectID
	--from	[IntersectContext] C
	--where	C.CompanyID = @CompanyID and C.IntersectID = @IntersectID
	--UNION ALL
	--select	P.ID,
	--		P.SourceIntersectContextID as ParentID,
	--		P.IntersectID
	--from	[IntersectContext] P
	--		inner join cte C on P.CompanyID = @CompanyID and C.ParentID = P.ID
	--)

	--insert into @tbl
	--	select	0 as ID,
	--			NULL as ParentID,
	--			ID as IntersectID
	--	from	[Intersect] 
	--	where	CompanyID = @CompanyID and ID = @IntersectID
	--	union
	--	select	ID,
	--			COALESCE(ParentID, 0) as ParentID,
	--			IntersectID
	--	from	cte

	select	--distinct
			T.ID,
			T.SourceIntersectContextID,
			T.IntersectID,
			COALESCE(O_D.Name, I.Name) as Name,
			O_N.ObjectType, 
			O_N.ObjectID,
			O_D.Url,
			C.Description,
			COALESCE(R.Name, 'None') as [Role],
			R.Description as RoleDescription,
			COALESCE(R.FontColor, '#000') as RoleFontColor,
			COALESCE(F.Name, 'None') as Classification,
			F.Description as ClassificationDescription,
			COALESCE(F.BorderColor, '#000') as ClassificationBorderColor,
			DI.DomainListItems
	from	(
			select	C.ID,
					C.IntersectRoleID,
					C.IntersectClassificationID,
					C.SourceIntersectContextID,
					C.IntersectID
			from	[IntersectContext] C
					inner join [IntersectContext] S on S.CompanyID = C.CompanyID and C.CompanyID = @CompanyID and C.SourceIntersectContextID = S.ID and S.IntersectID = @IntersectID
			union
			select	C.ID,
					S.IntersectRoleID,
					S.IntersectClassificationID,
					S.SourceIntersectContextID,
					S.IntersectID
			from	[IntersectContext] C
					inner join [IntersectContext] S on S.CompanyID = C.CompanyID and C.CompanyID = @CompanyID and C.SourceIntersectContextID = S.ID and C.IntersectID = @IntersectID		
			) T
			inner join [Intersect] I on I.CompanyID = @CompanyID and I.ID = T.IntersectID
			inner join IntersectNode B_N on B_N.CompanyID = I.CompanyID and B_N.IntersectID = I.ID
			inner join IntersectTypeNode BT_N on BT_N.CompanyID = B_N.CompanyID and BT_N.ID = B_N.IntersectTypeNodeID and BT_N.ObjectID = @TypeIDToRemove
			inner join IntersectNode O_N on O_N.CompanyID = I.CompanyID and O_N.IntersectID = I.ID
			inner join IntersectTypeNode OT_N on OT_N.CompanyID = O_N.CompanyID and OT_N.ID = O_N.IntersectTypeNodeID and OT_N.ObjectID <> @TypeIDToRemove
			outer apply utility.ObjectDetail(O_N.CompanyID, O_N.ObjectType, O_N.ObjectID) O_D
			left join IntersectContext C on C.CompanyID = I.CompanyID and C.ID = T.ID
			left join IntersectRole R on R.CompanyID = C.CompanyID and R.ID = T.IntersectRoleID
			left join IntersectClassification F on F.CompanyID = C.CompanyID and F.ID = C.IntersectClassificationID
			outer apply (
						select	(
								select	DLI.Name as Code,
										DL.Name as List
								from	IntersectContextDomainListItem CDLI
										inner join DomainListItem DLI on	CDLI.CompanyID = DLI.CompanyID 
																			and CDLI.DomainListItemID = DLI.ID
																			and CDLI.CompanyID = C.CompanyID
																			and CDLI.IntersectContextID = C.ID
										inner join DomainList DL on DL.CompanyID = DLI.CompanyID and DL.ID = DLI.DomainListID
								FOR XML PATH('item'), ROOT('codes'), ELEMENTS
								) as DomainListItems
						) DI

	--with cte as 
	--( 
	--select	distinct 
	--		I.CompanyID,
	--		IC.ID as IntersectContextID,
	--		IC.SourceIntersectContextID,
	--		I.ID as IntersectID,
	--		I.Name,
	--		IC.SourceIntersectContextID,
	--		IC.IntersectClassificationID,
	--		IC.IntersectRoleID,
	--		IC.Description,
	--		NULL as Parent
	--from	[Intersect] I
	--		left join [IntersectContext] IC on IC.CompanyID = I.CompanyID and IC.IntersectID = I.ID
	--where	I.CompanyID = @CompanyID and I.ID = @IntersectID
	--UNION ALL
	--select	I.CompanyID,
	--		IC.ID as IntersectContextID,
	--		IC.SourceIntersectContextID,
	--		I.ID as IntersectID,
	--		I.Name,
	--		IC.SourceIntersectContextID,
	--		IC.IntersectClassificationID,
	--		IC.IntersectRoleID,
	--		IC.Description,
	--		P.IntersectID as Parent
	--from	[Intersect] I
	--		inner join [IntersectContext] IC on IC.CompanyID = I.CompanyID and IC.IntersectID = I.ID
	--		inner join cte P on P.CompanyID = I.CompanyID and P.SourceIntersectContextID = IC.ID
	--)

	--SELECT	C.IntersectID,
	--		C.Parent,
	--		D.ParentType as TargetType,
	--		D.Name as TargetName,
	--		C.IntersectRoleID as RoleID,
	--		R.Name as [Role],
	--		R.FontColor as RoleFontColor,
	--		DI.DomainListItems
	--FROM	cte C
	--		left join IntersectClassification IC on IC.CompanyID = C.CompanyID and IC.ID = C.IntersectClassificationID
	--		left join IntersectRole R on R.CompanyID = C.CompanyID and R.ID = C.IntersectRoleID
	--		outer apply (
	--					select	(
	--							select	DLI.Name as Code,
	--									DL.Name as List
	--							from	IntersectContextDomainListItem CDLI
	--									inner join DomainListItem DLI on	CDLI.CompanyID = DLI.CompanyID 
	--																		and CDLI.DomainListItemID = DLI.ID
	--																		and CDLI.CompanyID = C.CompanyID
	--																		and (
	--																			CDLI.IntersectContextID = C.IntersectContextID OR
	--																			CDLI.IntersectContextID = C.SourceIntersectContextID
	--																			)
	--									inner join DomainList DL on DL.CompanyID = DLI.CompanyID and DL.ID = DLI.DomainListID
	--							FOR XML PATH('item'), ROOT('codes'), ELEMENTS
	--							) as DomainListItems
	--					) DI
	--		cross apply utility.ObjectDetail(@CompanyID, @type, C.IntersectID) D
END
