CREATE TABLE [dbo].[Report](
	[CompanyID] [int] NOT NULL,
	[ID] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](4000) NULL,
	[Layout] [varchar](4000) NOT NULL,
	[ObjectType] [varchar](25) NOT NULL,
	[ObjectID] [int] NOT NULL,
 CONSTRAINT [PK_Report] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

CREATE TABLE [dbo].[ReportGrid](
	[CompanyID] [int] NOT NULL,
	[ID] [int] NOT NULL,
	[ReportID] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[SQL] [varchar](8000) NULL,
	[Fields] [nvarchar](max) NULL,
	[Columns] [nvarchar](max) NULL,
	Location varchar(50) NOT NULL,
 CONSTRAINT [PK_ReportGrid] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[ReportGrid]  WITH CHECK ADD  CONSTRAINT [FK_ReportGrid_Report] FOREIGN KEY([CompanyID], [ReportID])
REFERENCES [dbo].[Report] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[ReportGrid] CHECK CONSTRAINT [FK_ReportGrid_Report]
GO



CREATE TABLE [dbo].[ReportChart](
	[CompanyID] [int] NOT NULL,
	[ID] [int] NOT NULL,
	[ReportID] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[SQL] [varchar](8000) NOT NULL,
	[Fields] [nvarchar](max) NOT NULL,
	[Series] [varchar](8000) NOT NULL,
	[CategoryDataField] varchar(250) NOT NULL,
	Location varchar(50) NOT NULL,
 CONSTRAINT [PK_ReportChart] PRIMARY KEY CLUSTERED 
(
	[CompanyID] ASC,
	[ID] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF)
)FEDERATED ON ([company_distribution] = [CompanyID])
GO

ALTER TABLE [dbo].[ReportChart]  WITH CHECK ADD  CONSTRAINT [FK_ReportChart_Report] FOREIGN KEY([CompanyID], [ReportID])
REFERENCES [dbo].[Report] ([CompanyID], [ID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[ReportChart] CHECK CONSTRAINT [FK_ReportChart_Report]
GO


INSERT INTO [Report] VALUES (4, 1, 'Event Count', 'Gives great event count!', '[{}]', 'ArtifactType', 1)

select * from ReportDefinition

INSERT INTO [dbo].[ReportGrid]
     VALUES
           (4
           ,1
           ,1
		   ,'Test Grid'
           ,'SELECT ProviderName, StartYear, EndYear, Count FROM [test].[BusinessTermStats] WHERE companyID = {CompanyID} and ArtifactID = {ObjectID} ORDER BY ProviderName'
           ,'[{ name: "ProviderName" }, { name: "StartYear" }, { name: "EndYear" }, { name: "Count" }]'
           ,'[{ datafield: "ProviderName", text: "ProviderName" }, { datafield: "StartYear", text: "CoverageStart", width: 200 }, { datafield: "EndYear", text: "CoverageEnd", width: 200 }, { datafield: "Count", text: "# Items", width: 120 }]'
           ,'1')

INSERT INTO ReportChart VALUES
           (4
           ,1
           ,1
		   ,'Test Chart'
           ,'select		convert(varchar,cast(DateCreated as smalldatetime),110) as x,
			Count(1) as y,
			''Event Count By Date'' as label
from		[Event]
where		CompanyID = 4
group by	convert(varchar,cast(DateCreated as smalldatetime),110)  
order by	convert(varchar,cast(DateCreated as smalldatetime),110)'
           ,'[{ name: "x" }, { name: "y" }, { name: "label" }]'
           ,'[
                {
                    type: ''splinearea'',
                    alignEndPointsWithIntervals: false,
                    valueAxis:
                    {
                        unitInterval: 25,
                        gridLinesInterval: 25,
                        gridLinesDashStyle: ''2,2'',
                        tickMarksColor: ''#ccc'',
                        displayValueAxis: true,
                        description: ''Index Value''
                    },
                    series: [
                        { dataField: ''y'', displayText: ''Event Count'', opacity: 0.7 }
                    ]
                }
            ]'
           ,'x'
           ,'2')
GO

INSERT INTO ReportChart VALUES
           (4
           ,2
           ,1
		   ,'Test Chart 2'
,'select	''11-01-2013'' as x, 10 as y1, 12 as y2
union
select	''11-02-2013'' as x, 14 as y1, 9 as y2
union
select	''11-03-2013'' as x, 20 as y1, 16 as y2
union
select	''11-04-2013'' as x, 8 as y1, 19 as y2
union
select	''11-05-2013'' as x, 32 as y1, 4 as y2
union
select	''11-06-2013'' as x, 24 as y1, 20 as y2
union
select	''11-07-2013'' as x, 13 as y1, 12 as y2
union
select	''11-08-2013'' as x, 17 as y1, 16 as y2
union
select	''11-09-2013'' as x, 22 as y1, 13 as y2
union
select	''11-10-2013'' as x, 29 as y1, 19 as y2
union
select	''11-11-2013'' as x, 26 as y1, 9 as y2
union
select	''11-12-2013'' as x, 22 as y1, 8 as y2
union
select	''11-13-2013'' as x, 10 as y1, 12 as y2
union
select	''11-14-2013'' as x, 14 as y1, 9 as y2
union
select	''11-15-2013'' as x, 10 as y1, 12 as y2'
           ,'[{ name: "x" }, { name: "y1" }, { name: "y2" }]'
           ,'[
                {
                    type: ''splinearea'',
                    alignEndPointsWithIntervals: false,
                    valueAxis:
                    {
                        unitInterval: 25,
                        gridLinesInterval: 25,
                        gridLinesDashStyle: ''2,2'',
                        tickMarksColor: ''#ccc'',
                        displayValueAxis: true,
                        description: ''Index Value''
                    },
                    series: [
                        { dataField: ''y1'', displayText: ''Event Count'', opacity: 0.7 },
						{ dataField: ''y2'', displayText: ''Event Count over Last 15 Days'', opacity: 0.7 }
                    ]
                }
            ]'
           ,'x'
           ,'2')