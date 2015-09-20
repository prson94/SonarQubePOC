select	A.Name,
		E.EventCount, 
		E.Type
from	MonitorArtifactType M 
		INNER JOIN ArtifactType AT ON M.CompanyID = AT.CompanyID AND M.ID = AT.ID
		INNER JOIN Artifact A ON A.CompanyID = AT.CompanyID AND A.ArtifactTypeID = AT.ID
		CROSS APPLY dbo.GetEventCountsBySourceObject(A.CompanyID, 'Artifact', A.ID) E


select * from EventAssignment

INSERT into EventAssignment VALUES (1,3,6,'R',1, 1, NULL, getutcdate(), 1)