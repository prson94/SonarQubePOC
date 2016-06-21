--add column ReportType default to legacy for powerbi/legacy reports
alter table Report add  [ReportType] Varchar(25) CONSTRAINT [DF_Report_ReportType] DEFAULT (('legacy')) NOT NULL
-- add column GUID
alter table Report add  [PowerBIReportID] Varchar(50) NULL
alter table Report add  [PowerBIDatasetID] Varchar(50) NULL