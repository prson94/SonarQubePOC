export enum ReportType {
    legacy,
    powerbi
}

export class Report {
    ID: number;
    Name: string;
    Description: string;
    ObjectID: number;
    ObjectType: string;    
    PowerBIDatasetID: string;
    PowerBIReportID: string;
    ReportLayoutID: number;
    ReportType: string;
}

export class ReportTile {
    ID: number;
    Name: string;
    ContentAreaNumber;
    ReportID: number;
    ReportTileType: number;
    CommandText: string;
    Settings: string;
}

export enum ReportTileTypes {
    Table = 1,    
    Pie = 2,    
    Area = 3,    
    Bar = 4,    
    Line = 5,    
    Matrix = 6
}

export class ReportLayoutTile {
    ID: number;
    Name: string;
    ReportTileType: ReportTileTypes;
    Icon: string;
}

export class ReportLayoutArea {    
    height: number;
    id: number;
    tiles: ReportLayoutTile[];
}

export class ReportLayoutCell {
    areas: ReportLayoutArea[];
}

export class ReportLayout {
    cells: ReportLayoutCell[];
}