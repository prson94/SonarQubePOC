create VIEW [dbo].[WorkflowIssue]
AS
(
(select		W.ID as WorkflowID
			,-1 as WorkflowItemID
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
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Notes			
			,C.Body as Comments		
			,case when W.Data.value('(fields/IssueType)[1]', 'int') is null then 0 else W.Data.value('(fields/IssueType)[1]', 'int') end as IssueType
			,case when W.Data.value('(fields/IssueType)[1]', 'int') = 0 then 'Business Data Incorrect' else 'Governance Information Incorrect' end as IssueTypeName
			,0 as IssueID
			,2 as Criticality
			,'Medium' as CriticalityName
			,case when W.DateCompleted is null then datediff(day,W.DateStarted,GetUtcDate()) else datediff(day, W.DateStarted, W.DateCompleted) end as EllapsedDays
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			left outer join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.recordnumber = 7
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')			
            where  W.WorkflowType = 3 and W.Data.value('(fields/IssueID)[1]', 'int') is null)
)
union
(select		W.ID as WorkflowID
			,-1 as WorkflowItemID
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
			,ws.data.value('(fields/Comment)[1]','nvarchar(500)') as Notes	
			,C.Body as Comments				
			,IT.ID as IssueType
            ,IT.Name as IssueTypeName
			,I.ID as IssueID
			,I.Criticality as Criticality
			,case when I.Criticality = 0 then 'Negligible' when I.Criticality = 1 then 'Low' when I.Criticality = 2 then 'Medium' when I.Criticality = 3 then 'High'  when I.Criticality = 4 then 'Critical' else 'N/A' end as CriticalityName
			,case when W.DateCompleted is null then datediff(day,W.DateStarted,GetUtcDate()) else datediff(day, W.DateStarted, W.DateCompleted) end as EllapsedDays
from	    Workflow W		
			inner join Comment C on C.ID = W.Data.value('(fields/CommentID)[1]', 'int')
			inner join Issue I on (I.ID = W.Data.value('(fields/IssueID)[1]', 'int'))
			inner join IssueType IT on (I.IssueTypeID = IT.ID)			
			left outer join CommentRelation CR on CR.CommentID = C.ID and CR.ObjectType not in ('Resource', 'Group')
			left outer join workflowstatus ws on w.id = ws.workflowid and ws.recordnumber = 7
			left outer join cache.ObjectDetails A on A.[Object] = CR.ObjectType and A.ObjectID = CR.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = W.Data.value('(fields/ResourceID)[1]', 'int')			
            where  W.WorkflowType = 3
)
union
(select		null as WorkflowID
			,wi.ID as WorkflowItemID
		    ,-1 as CommentID
			,I.CreatedBy as CreatingResourceID
			,wi.StartedOn
			,wi.CompletedOn
			,'' as Step
			,A.ObjectID
			,A.Name
			,A.[Object]
			,A.Url
			,R.FirstName + ' ' + R.LastName as RaisedBy			
			,case when wi.CompletedOn is null then cast(0 as bit) else cast(1 as bit) end as IsCompleted	
			,'' as Notes					
			,'' as Comments
			,IT.ID as IssueType
            ,IT.Name as IssueTypeName
			,I.ID as IssueID
			,I.Criticality as Criticality
			,case when I.Criticality = 0 then 'Negligible' when I.Criticality = 1 then 'Low' when I.Criticality = 2 then 'Medium' when I.Criticality = 3 then 'High'  when I.Criticality = 4 then 'Critical' else 'N/A' end as CriticalityName
			,case when wi.CompletedOn is null then datediff(day,wi.StartedOn,GetUtcDate()) else datediff(day, wi.StartedOn, wi.CompletedOn) end as EllapsedDays
from	    Issue I
			inner join [workflow].item wi on (wi.[object] = 'Issue' and wi.[objectid] = i.id)
			inner join IssueType IT on (I.IssueTypeID = IT.ID)						
			left outer join cache.ObjectDetails A on A.[Object] = I.ObjectType and A.ObjectID = I.ObjectID            		
			left outer join reporting.Global_Resource R on R.ResourceID = I.CreatedBy
)


GO