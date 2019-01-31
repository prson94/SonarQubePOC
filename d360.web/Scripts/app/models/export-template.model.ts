export class ExportTemplate {
    Name: string;    
    Description: string;
    ID: number;
    AssetTypeID: number;
    AssetTypeUID: string;
    IncludeFields: string;
    HasTemplateFile: boolean;
    ExportViewType: ExportViewType;
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
