export interface IObjectDetailService {
    getObjectDetail(objectID: number, objectType: string): Promise<DetailModel>;
}

export class DetailModel {
    columns: number;
    rows: DetailRow[];
}

export class DetailRow {
    Category: any;
    columns: number;
    FirstColumnFields = new Array<DetailField>();
    SecondColumnFields = new Array<DetailField>();
}

export class DetailField {
    Column: any;
    FieldDescription: string;
    FieldName: string;
    Group: any;
    HideFooter: boolean;
    HideHeader: boolean;
    LookupGridUrl: string;
    MultipleValues: any;
    Name: string;
    Row: any;
    ScriptProperty: any;
    TooltipContext: any;
    TooltipID: any;
    TooltipType: any;
    TooltipUrl: string;
    Value: string;
}