create procedure [tile].[GetSocialStatisticsByObject]
--declare
	@type varchar(50),
	@id int
--set @type = 'Artifact'
--set @id = 733
as
begin
	select	F.C as FollowerCount,
			P.C as CommentCount,
			LP.C as CommentCountLast48Hours
	from	(
			select	count(1) as C
			from	Follow
			where	ObjectType = @type
					and ObjectID = @id
			) F
			cross apply
			(
			select	count(C.ID) as C
			from	Comment C
					inner join CommentRelation R	on R.CommentID = C.ID 
													and R.ObjectType = @type 
													and R.ObjectID = @id
													and C.IsDeleted = 0
			) P
			cross apply
			(
			select	count(C.ID) as C
			from	Comment C
					inner join CommentRelation R	on R.CommentID = C.ID 
													and R.ObjectType = @type 
													and R.ObjectID = @id
													and C.DateCreated > dateadd(dd, -2, getutcdate())
													and C.IsDeleted = 0
			) LP
end