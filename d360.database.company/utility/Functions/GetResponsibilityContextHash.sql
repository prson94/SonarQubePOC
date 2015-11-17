CREATE FUNCTION [utility].[GetResponsibilityContextHash]
(
	@ID int
)
RETURNS varchar(50)
AS
BEGIN
	DECLARE @hash varchar(50)


	DECLARE @HashThis nvarchar(1000);
	SELECT @HashThis = coalesce(STUFF((SELECT ';' + cast(DI.ID as nvarchar(10))
			  FROM ResponsibilityContextItem RCI
					inner join DomainItem DI on DI.ID = RCI.ObjectID and RCI.ObjectType = 'DomainItem' and RCI.ResponsibilityID = @ID
			  ORDER BY DI.ID
			  FOR XML PATH('')), 1, 1, ''), '')

	SELECT @hash = CONVERT(Char,HashBytes('SHA1', @HashThis),2) --SUBSTRING(master.dbo.fn_varbintohexstr(HashBytes('SHA1', @HashThis)), 3, 32)

	RETURN @hash
END