--CREATE FEDERATION customer_federation (customer_distribution bigint RANGE)
--USE FEDERATION ROOT WITH RESET
--USE FEDERATION customer_federation (customer_distribution = 1000) WITH FILTERING=OFF, RESET  --{ON|OFF}
CREATE TABLE Customer
( 
	ID bigint not null,
	Name nvarchar(250),
	CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED 
	(
		ID ASC
	)
) 
FEDERATED ON (customer_distribution = ID)
GO

ALTER TABLE Customer
ADD	Name nvarchar(250) not null
GO

/*
USE FEDERATION ROOT WITH RESET
GO
ALTER FEDERATION customer_federation SPLIT AT (customer_distribution=500)
GO
*/