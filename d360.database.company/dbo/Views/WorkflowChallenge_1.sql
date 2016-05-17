CREATE VIEW [dbo].[WorkflowChallenge]
AS
select		W.ID as WorkflowID
		    ,W.Data.value('(fields/CommentID)[1]', 'int') as CommentID
			,W.Data.value('(fields/RequestingResourceID)[1]', 'int') as CreatingResourceID
			,W.Data.value('(fields/ArtifactTypeID)[1]', 'int') as ArtifactTypeID
			,W.Data.value('(fields/ArtifactTypeName)[1]', 'nvarchar(250)') as ArtifactTypeName
			,W.Data.value('(fields/ArtifactID)[1]', 'int') as ArtifactID
			,W.Data.value('(fields/Name)[1]', 'nvarchar(250)') as Name
			,'#/artifacts/' + cast(W.Data.value('(fields/ArtifactTypeID)[1]', 'int') as varchar) + '/' + cast(W.Data.value('(fields/ArtifactID)[1]', 'int') as varchar) as Url
			,W.DateStarted
			,W.DateCompleted	
			,W.Step						
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when w.dateCompleted is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,ws.Data.value('(fields/Approved)[1]', 'bit') as Approved		
			,ws.Data.value('(fields/Note)[1]', 'nvarchar(500)') as ClosingNotes
			,R_a.FirstName + ' ' + R_a.LastName as ClosedBy			
			,ws.Data.value('(fields/ApproverResourceID)[1]', 'int') as ClosedByResourceID
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')			
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/RequestingResourceID)[1]', 'int')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.activityname = 'Read Approval'
			left outer join reporting.Global_Resource R_a on R_a.ResourceID = ws.Data.value('(fields/ApproverResourceID)[1]', 'int')
            where  W.WorkflowType = 4