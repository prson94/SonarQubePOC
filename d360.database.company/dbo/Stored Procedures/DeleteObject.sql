CREATE procedure [dbo].[DeleteObject]
	@Obj varchar(50),
	@ObjectID int,
	@ResourceID int
as
begin
	set nocount on;
	
	declare @Object varchar(50) = @Obj,
			@trans varchar(25) = 'Trans',
			@current int = 1,
			@max int

	begin try
		begin transaction @trans

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        VALUES (
				'ObjectVersion', 
				'<fields>
				 <Action>Removed</Action>
				 <ActionObject>' + @Obj + '</ActionObject>
				 <ActionObjectID>' + cast(@ObjectID as varchar) + '</ActionObjectID>
				 <ResourceID>' + cast(@ResourceID as varchar) + '</ResourceID>
				</fields>', 
				@Obj, 
				@ObjectID)

		INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
		values ('ObjectIndex', 'D', @Obj, @ObjectID)

		--COMMON
		delete CommentRelation					where ObjectType = @Object and ObjectID = @ObjectID
		delete Field							where ObjectType = @Object and ObjectID = @ObjectID
		delete Follow							where ObjectType = @Object and ObjectID = @ObjectID
		delete Responsibility					where ObjectType = @Object and ObjectID = @ObjectID
		delete SurveyObjectCache				where ObjectType = @Object and ObjectID = @ObjectID
		delete cache.[Object]					where [Object] = @Object and ObjectID = @ObjectID

		if charindex('Type', @Object) > 0
		begin
			delete AttributeTypeRelation			where ObjectType = @Object AND ObjectID = @ObjectID
			delete FieldType						where [Object] = @Object AND ObjectID = @ObjectID
			delete ResponsibilityTypeRelation		where ObjectType = @Object and ObjectID = @ObjectID
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Object and ObjectID = @ObjectID
			delete StatisticType					where [Object] = @Object and [ObjectID] = @ObjectID
			delete WorkflowTypeRelation				where [Object] = @Object and ObjectID = @ObjectID

			if @Object = 'ArtifactType'
			begin
				declare @ah table (ID int);
				with ah as	(
							select	ID, 
									ParentID
							from	Artifact
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Artifact C
									inner join ah P on P.ID = C.ParentID
							)
				insert into @ah 
					select ID from ah
			
				delete Artifact where ID in (select ID from @ah)
			end

			if @Object = 'AttributeType'
			begin
				delete AttributeTypeRelation		where AttributeTypeID = @ObjectID
			
				declare @ath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with ath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	AttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	AttributeType C
									inner join ath P on P.ID = C.ParentID
							)
				insert into @ath 
					select ID, ParentID, [Level] from ath order by [Level] desc

				select @max = max(RowID) from @ath

				while @current <= @max
				begin
					declare @attributeTypeID int
					select @attributeTypeID = ID from @ath where RowID = @current
					delete Attribute where AttributeTypeID = @attributeTypeID
					delete AttributeType where ID = @attributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'DomainType'
			begin
				delete DomainItem where DomainID in (select ID from Domain where DomainTypeID = @ObjectID)
				delete Domain where DomainTypeID = @ObjectID
				delete DomainGroup where DomainTypeID = @ObjectID
			end

			if @Object = 'FieldType'
			begin
				delete Field where FieldTypeID = @ObjectID
				delete FieldType where ID = @ObjectID
			end

			if @Object = 'FusionAttributeType'
			begin
				declare @fath table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fath as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fath P on P.ID = C.ParentID
							)
				insert into @fath 
					select ID, ParentID, [Level] from fath order by [Level] desc

				select @max = max(RowID) from @fath

				while @current <= @max
				begin
					declare @fusionAttributeTypeID int
					select @fusionAttributeTypeID = ID from @fath where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fusionAttributeTypeID
					delete FusionAttributeType where ID = @fusionAttributeTypeID
					set @current = @current + 1
				end
			end

			if @Object = 'FusionType'
			begin
				declare @fth table (RowID int identity, ID int, ParentID int null, [Level] int);
				with fth as	(
							select	ID,
									ParentID,
									1 as [Level]
							from	FusionAttributeType
							where	FusionTypeID = @ObjectID and ParentID is null
							union all
							select	C.ID,
									C.ParentID,
									P.[Level] + 1 as [Level]
							from	FusionAttributeType C
									inner join fth P on P.ID = C.ParentID
							)
				insert into @fth 
					select ID, ParentID, [Level] from fth order by [Level] desc

				select @max = max(RowID) from @fth

				while @current <= @max
				begin
					declare @fattributeTypeID int
					select @fattributeTypeID = ID from @fth where RowID = @current
					delete FusionAttribute where FusionAttributeTypeID = @fattributeTypeID
					delete FusionAttributeType where ID = @fattributeTypeID
					set @current = @current + 1
				end
				delete FusionType where ID = @ObjectID
			end

			if @Object = 'IntersectType'
			begin
				-- Stores the sources we have identified through the loop below.
				declare @tblRelationshipIDs table (ID int)

				--Seed initial tables values
				insert into @tblRelationshipIDs
					select	R.ID 
					from	Responsibility R
							inner join [Intersect] I on I.IntersectTypeID = 2 and R.ObjectType = 'Intersect' and R.ObjectID = I.ID 

				-- follow trail all the way back.
				while exists(
						select	1 
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				)
				begin
					insert into @tblRelationshipIDs
						select	ID
						from	Responsibility
						where	TargetResponsibilityID in (select ID from @tblRelationshipIDs)
								and ID not in (select ID from @tblRelationshipIDs)
				end

				delete Responsibility where ID in (select ID from @tblRelationshipIDs)

				delete [Intersect] where IntersectTypeID = @ObjectID
				delete IntersectType where ID = @ObjectID
			end

			if @Object = 'LookupType'
			begin
				delete [Lookup] where LookupTypeID = @ObjectID
			end

			if @Object = 'PolicyType'
			begin
				delete Policy where PolicyTypeID = @ObjectID
				delete PolicyTypeLevel where PolicyTypeID = @ObjectID
			end

			if @Object = 'ResponsibilityType'
			begin
				delete Responsibility where ResponsibilityTypeID = @ObjectID
				delete ResponsibilityType where ID = @ObjectID
			end

			--if @Object = 'StatisticType'
			--begin
			--	delete [Statistic] where StatisticTypeID = @ObjectID
			--end

			if @Object = 'SurveyType'
			begin
				delete SurveyObjectCache where SurveyTypeID = @ObjectID
				delete Survey where SurveyTypeID = @ObjectID
				delete SurveyType where ID = @ObjectID
			end

			if @Object = 'TaxonomyType'
			begin
				delete Taxonomy where TaxonomyTypeID = @ObjectID
				delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
				delete TaxonomyType where ID = @ObjectID
			end

		end
		else
		begin
			delete Attribute							where ObjectType = @Object and ObjectID = @ObjectID
			delete cache.Relationship					where [SourceObject] = @Object and SourceObjectID = @ObjectID
			delete cache.Relationship					where [TargetObject] = @Object and TargetObjectID = @ObjectID

			BEGIN TRY
				DECLARE @tblIntersectIDs table (ID int)

				INSERT INTO @tblIntersectIDs
					SELECT	IntersectID
					FROM	IntersectNode
					WHERE	ObjectType = @Object and ObjectID = @ObjectID

				delete [Intersect] where ID in (select ID from @tblIntersectIDs)
			END TRY
			BEGIN CATCH

			END CATCH

			if @Object = 'Artifact'
			begin
				delete	RelatedArtifact where ArtifactID = @ObjectID
			end

			if @Object = 'Domain'
			begin
				delete DomainItem where DomainID = @ObjectID
			end

			if @Object = 'Taxonomy'
			begin
				declare @th table (ID int);
				with th as	(
							select	ID, 
									ParentID
							from	Taxonomy
							where	ID = @ObjectID
							union all
							select	C.ID,
									C.ParentID
							from	Taxonomy C
									inner join th P on P.ID = C.ParentID
							)
				insert into @th 
					select ID from th
			
				delete Taxonomy where ID in (select ID from @th)
			end
		end
		
		commit transaction @trans
	end try
	begin catch
		 DECLARE @ErrorMessage NVARCHAR(4000);
    DECLARE @ErrorSeverity INT;
    DECLARE @ErrorState INT;

    SELECT 
        @ErrorMessage = ERROR_MESSAGE(),
        @ErrorSeverity = ERROR_SEVERITY(),
        @ErrorState = ERROR_STATE();

    -- Use RAISERROR inside the CATCH block to return error
    -- information about the original error that caused
    -- execution to jump to the CATCH block.
    RAISERROR (@ErrorMessage, -- Message text.
               @ErrorSeverity, -- Severity.
               @ErrorState -- State.
               );

		rollback transaction @trans
	end catch
end

