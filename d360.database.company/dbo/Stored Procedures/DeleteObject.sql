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

		declare @h table (ID int);
		declare @IsType bit = 0
		declare @ht table (ID int);

		if charindex('Type', @Object) > 0
		begin
			set @IsType = 1
		end

		if @Object = 'ArtifactType'
		begin
			with ht as	(
						select	ID, 
								ParentID
						from	ArtifactType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	ArtifactType C
								inner join ht P on P.ID = C.ParentID
						)
			insert into @ht 
				select ID from ht

			insert into @h
				select ID from Artifact where ArtifactTypeID in (select ID from @ht)
			
			delete ArtifactTypeExportTemplate where ArtifactTypeID in (select ID from @ht)
			delete Artifact where ID in (select ID from @h)

			--set @Object = 'Artifact'
		end

		if @Object = 'AttributeType'
		begin
			with ht as	(
						select	ID, 
								ParentID
						from	AttributeType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	AttributeType C
								inner join ht P on P.ID = C.ParentID
						)
			insert into @ht 
				select ID from ht

			insert into @h
				select ID from Attribute where AttributeTypeID in (select ID from @ht)

			--set @Object = 'Artifact'
		end

		if @Object = 'FieldType'
		begin
			delete Field where FieldTypeID = @ObjectID
			delete FieldType where ID = @ObjectID
		end

		if @Object = 'FusionAttributeType'
		begin
			with ht as	(
						select	ID, 
								ParentID
						from	FusionAttributeType
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	FusionAttributeType C
								inner join ht P on P.ID = C.ParentID
						)
			
			insert into @ht 
				select ID from ht

			insert into @h
				select ID from FusionAttribute where FusionAttributeTypeID in (select ID from @ht)
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
			delete PolicyType where ID = @ObjectID
		end

		if @Object = 'ReferenceItemType'
		begin
			delete ReferenceItem where ReferenceItemTypeID = @ObjectID
			delete ReferenceItemType where ID = @ObjectID
		end

		if @Object = 'ResponsibilityType'
		begin
			delete ResponsibilityTypeRelationOverrideItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationItem where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityTypeRelationRule where ResponsibilityTypeID = @ObjectID
			delete ResponsibilityType where ID = @ObjectID
		end

		if @Object = 'RuleType'
		begin
			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultQualifierType F
					inner join RuleImplementation I on I.ID = F.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID
					inner join [Rule] R on R.ID = I.RuleID and R.RuleTypeID = @ObjectID

			delete [RuleImplementation] where RuleID in (select ID from [Rule] where RuleTypeID = @ObjectID)

			delete [Rule] where RuleTypeID = @ObjectID

			delete RuleType where ID = @ObjectID
		end

		if @Object = 'SurveyType'
		begin
			--delete SurveyObjectCache where SurveyTypeID = @ObjectID
			delete Survey where SurveyTypeID = @ObjectID
			delete SurveyType where ID = @ObjectID
		end

		if @Object = 'TaxonomyType'
		begin
			delete Taxonomy where TaxonomyTypeID = @ObjectID
			delete TaxonomyTypeLevel where TaxonomyTypeID = @ObjectID
			delete TaxonomyType where ID = @ObjectID
		end

		if @Object = 'Artifact'
		begin
			-- HIERARCHY
			with h as	(
						select	ID, 
								ParentID
						from	Artifact
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	Artifact C
								inner join h P on P.ID = C.ParentID
						)
			insert into @h 
				select ID from h

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	@Object, 
						O.ID, 
						O.DisplayValue, 
						coalesce(O.UpdatedBy, 0), 
						coalesce(O.UpdatedOn, getutcdate()), 
						'Deleted', 
						@Object, 
						O.ID, 
						T.Name, 
						O.DisplayValue, 
						'This artifact has been removed.' 
				from	Artifact O
						inner join @h I on I.ID = O.ID 
						inner join ArtifactType T on T.ID = O.ArtifactTypeID;
			
			-- DELETE
			delete Artifact where ID in (select ID from @h)
		end

		if @Object = 'Policy'
		begin
			-- HIERARCHY
			with th as	(
						select	ID, 
								ParentID
						from	[Policy]
						where	ID = @ObjectID
						union all
						select	C.ID,
								C.ParentID
						from	[Policy] C
								inner join th P on P.ID = C.ParentID
						)

			insert into @h 
				select ID from th

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	@Object, 
						O.ID, 
						O.DisplayValue, 
						coalesce(O.UpdatedBy, 0), 
						coalesce(O.UpdatedOn, getutcdate()), 
						'Deleted', 
						@Object, 
						O.ID, 
						T.Name, 
						O.DisplayValue, 
						'This policy has been removed.' 
				from	[Policy] O
						inner join @h I on I.ID = O.ID 
						inner join PolicyType T on T.ID = O.PolicyTypeID;

			-- DELETE
			delete [Policy] where ID in (select ID from @h)
		end

		if @Object = 'Rule'
		begin
			insert into @h values (@ObjectID)

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	@Object, 
						O.ID, 
						O.DisplayValue, 
						coalesce(O.UpdatedBy, 0), 
						coalesce(O.UpdatedOn, getutcdate()), 
						'Deleted', 
						@Object, 
						O.ID, 
						T.Name, 
						O.DisplayValue, 
						'This rule has been removed.' 
				from	[Rule] O
						inner join @h I on I.ID = O.ID 
						inner join RuleType T on T.ID = O.RuleTypeID;
			-- DELETE
			delete T
			from   RuleResultQualifierType T
				   inner join RuleImplementation I on I.ID = T.RuleImplementationID and I.RuleID in (select ID from @h)

			delete	Q
			from	RuleResultQualifier Q 
					inner join RuleResult S on S.ID = Q.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID in (select ID from @h)

			delete	F
			from	RuleResultFusionAttribute F
					inner join RuleResult S on S.ID = F.RuleResultID
					inner join RuleImplementation I on I.ID = S.RuleImplementationID and I.RuleID in (select ID from @h)

			delete	RuleImplementation where RuleID in (select ID from @h)

			delete	[Rule] where ID in (select ID from @h)
		end

		if @Object = 'Taxonomy'
		begin
			-- HIERARCHY
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

			insert into @h 
				select ID from th

			-- AUDIT
			insert into reporting.Global_Audit (Object, ObjectID, ObjectName, ResourceID, Date, Action, ActionObject, ActionObjectID, ActionObjectTypeName, ActionObjectName, ActionDescription)
				select	@Object, 
						O.ID, 
						O.DisplayValue, 
						coalesce(O.UpdatedBy, 0), 
						coalesce(O.UpdatedOn, getutcdate()), 
						'Deleted', 
						@Object, 
						O.ID, 
						T.Name, 
						O.DisplayValue, 
						'This model has been removed.' 
				from	[Taxonomy] O
						inner join @h I on I.ID = O.ID 
						inner join TaxonomyType T on T.ID = O.TaxonomyTypeID;
			
			-- DELETE
			delete Taxonomy where ID in (select ID from @h)
		end

		-- Final check to see if @h table variable has anything in it form above. If not, auto-insert the curent ObjectID we got from the procedure parameters.
		if not exists(select ID from @h)
		begin
			insert into @h values (@ObjectID)
		end

		-- DELETE (Supporting Tables) ---------------------------------------------------------

		-- Delete attributes for these items.
		BEGIN TRY
			delete Attribute where ObjectType = @Object and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		BEGIN TRY
			DECLARE @tblIntersectIDs table (ID int)

			INSERT INTO @tblIntersectIDs
				SELECT	ID
				FROM	[Intersect]
				WHERE	(Subject = @Object and SubjectID in (select ID from @h)) OR (Object = @Object and ObjectID in (select ID from @h))

			--delete	MapItem 
			--where	SourceIntersectID in (select ID from @tblIntersectIDs) OR
			--		TargetIntersectID in (select ID from @tblIntersectIDs)

			delete [Intersect] where ID in (select ID from @tblIntersectIDs)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Comment deletion
		BEGIN TRY
			delete	Comment
			where	OwnerObjectType = @Object 
					and OwnerObjectID in (select ID from @h)

			delete	CommentRelation
			where	ObjectType = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Favorite deletion
		BEGIN TRY
			delete	Favorite
			where	Object = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Field deletion
		BEGIN TRY
			delete	Field
			where	ObjectType = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Follow deletion
		BEGIN TRY
			delete	Follow
			where	ObjectType = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Issue deletion
		BEGIN TRY
			delete	Issue
			where	Object = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Nym deletion
		BEGIN TRY
			delete	Nym
			where	Object = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- NymRelation deletion
		BEGIN TRY
			delete	NymRelation
			where	Object = @Object 
					and ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH

		-- Responsibility deletion
		BEGIN TRY
			delete T
			from	ResponsibilityTypeRelationOverrideItem T
					inner join Asset A on A.ID = T.AssetID and A.Object = @Object and A.ObjectID in (select ID from @h)

			delete T
			from	ResponsibilityTypeRelationItem T
					inner join Asset A on A.ID = T.AssetID and A.Object = @Object and A.ObjectID in (select ID from @h)
		END TRY
		BEGIN CATCH

		END CATCH
		---------------------------------------------------------------------------------------

		if @IsType = 1
		begin
			delete AttributeTypeRelation			where ObjectType = @Obj AND ObjectID in (select ID from @ht)
			delete FieldType						where Object = @Obj AND ObjectID in (select ID from @ht)
			delete IntersectType					where (Object = @Obj and ObjectID in (select ID from @ht)) OR (Subject = @Obj and SubjectID in (select ID from @ht))
			delete ObjectStyle						where ObjectType = @Obj AND ObjectID in (select ID from @ht)
			delete ResponsibilityTypeRelation		where ObjectType = @Obj and ObjectID in (select ID from @ht)
			delete ResponsibilityTypeObjectClaim	where ObjectType = @Obj and ObjectID in (select ID from @ht)

			if @Obj = 'ArtifactType'
			begin
				delete ArtifactType			where ID in (select ID from @ht)
			end
			if @Obj = 'AttributeType'
			begin
				delete AttributeTypeRelation	where AttributeTypeID in (select ID from @ht)
				delete AttributeType			where ID in (select ID from @ht)
			end
			if @Obj = 'FusionAttributeType'
			begin
				delete FusionAttribute		where FusionAttributeTypeID in (select ID from @ht)
				delete FusionAttributeType	where ID in (select ID from @ht)
			end
			if @Obj = 'PolicyType'
			begin
				delete PolicyType			where ID in (select ID from @ht)
			end
			if @Obj = 'RuleType'
			begin
				delete RuleType				where ID in (select ID from @ht)
			end
			if @Obj = 'TaxonomyType'
			begin
				delete TaxonomyType			where ID in (select ID from @ht)
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
GO

