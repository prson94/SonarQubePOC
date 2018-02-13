CREATE FUNCTION [dbo].[GetWorkflowResponsibleUsers]
(
	@itemStepId int,
	@firstResponse bit
)
RETURNS varchar(max)
AS
BEGIN
RETURN (
	
	select coalesce(string_agg(X.ResponsibleUsers, ', '), '[unknown]') as ResponsibleUsers from
	(
		select distinct
			case when @firstResponse = 1 then
					GR.FirstName + ' ' + GR.LastName
			else
				coalesce(
					GR2.FirstName + ' ' + GR2.LastName,
					GR.FirstName + ' ' + GR.LastName, 
					NULL)
			end as ResponsibleUsers
		from	workflow.ItemStep IST
		left join workflow.Item I on I.ID = IST.ItemID
		left join workflow.ItemAssignment IA on IA.ItemID = I.ID	
		left join reporting.Global_resource GR on GR.ResourceID = IST.CompletedBy
		left join reporting.Global_resource GR2 on GR2.ResourceID = IA.ResourceObjectID
		where
			IST.ID = @itemStepId
		group by GR.FirstName, GR.LastName, GR2.FirstName, GR2.LastName, IST.ID ,IST.ItemID, IST.StepID, IA.ID
	) X		
)
END