declare @companyID int
set @companyID = 1

delete Ownership where CompanyID = @companyID and ID in (
select		min(ID)
from		Ownership O
inner join	(
			select		OwnershipTypeID, 
						ObjectID, 
						ResourceObjectType, 
						ResourceOBjectID, 
						count(1) as c 
			from		Ownership 
			where		companyID = @companyID
			group by	OwnershipTypeID, ObjectID, ResourceObjectType, ResourceOBjectID 
			having		count(1) > 1
			) d on d.OwnershipTypeID = o.OwnershipTypeID
				and d.ObjectID = o.ObjectID
				and d.ResourceObjectType = o.ResourceObjectType
				and d.ResourceOBjectID = o.ResourceOBjectID
				and o.CompanyID = @companyID
group by	o.OwnershipTypeID, o.ObjectID, o.ResourceObjectType, o.ResourceOBjectID 
)