CREATE TABLE [dbo].[CompanyResource] (
    [CompanyID]       INT NOT NULL,
    [ResourceID]      INT NOT NULL,
    [IsAdministrator] BIT NOT NULL,
    CONSTRAINT [PK_CompanyResource] PRIMARY KEY CLUSTERED ([CompanyID] ASC, [ResourceID] ASC)
);


GO
CREATE TRIGGER [dbo].[CompanyResource_After]
   ON  [dbo].[CompanyResource] 
   AFTER INSERT, UPDATE, DELETE
AS 
	SET NOCOUNT ON;
	update	CacheStatus
	set		ShouldRecache = 1
	where	Name = 'Users'
