CREATE VIEW [dbo].[WorkflowIssue]
AS
select		W.ID as WorkflowID
		    ,W.Data.value('(fields/CommentID)[1]', 'int') as CommentID
			,W.Data.value('(fields/ResourceID)[1]', 'int') as CreatingResourceID
			,W.DateStarted
			,W.DateCompleted	
			,W.Step
			,A.ObjectID
			,A.Name
			,A.[Object]
			,A.Url
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when w.dateCompleted is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Comments					
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			left outer join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.recordnumber = 7
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')			
            where  W.WorkflowType = 3
GO