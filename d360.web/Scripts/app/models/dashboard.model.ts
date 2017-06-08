export class Dashboard {
    Name: string;
    ID: number;
    Description: string;
    ObjectID: number;
    ObjectType: string;
    PowerBIReportID: string;
    ReportType: string;
    Url: string;
}

export class PowerBIReport {
    embedUrl: string;
    id: string;
    name: string;
    webUrl: string;
}

export class DashboardTokens {
    AccessToken: string;
    Report: PowerBIReport;
}