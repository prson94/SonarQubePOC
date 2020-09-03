export class ExportTemplate {
    Name: string;    
    Description: string;
    ID: number;
    AssetTypeUID: string;
    IncludeFieldTypes: string[];
    HasTemplateFile: boolean;
    ExportViewType: ExportViewType;
    Uid: string;
}

export class ExportTemplateStyle {
    Column: number;
    Row: number;
    TextColor: string;
    BgColor: string;
    IsBold: boolean;
    AssetTypeExportTemplateID: number;
    ID: number;
    SelectionType: string;
}

export enum ExportViewType {
    None = 0,
    Pivot = 1,
    Grouped = 2
}
