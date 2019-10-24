export interface IObjectDetailService {
    getObjectDetail(objectID: number, objectType: string): Promise<DetailModel>;
}

export class AssetDetail {
    ID: number;
    DisplayValue: string;
    AssetTypeID: number;
    TypeID: number;
    Type: string;
    AssetTypeName: string;
    State: number;
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

export enum DetailFieldType {
    Field,
    Lookup,
    Tooltip,
    None,
    Hidden,
}

export class DetailSubField {
    TooltipContext: any;
    TooltipID: any;
    TooltipType: any;
    TooltipUrl: string;
    Value: string;
}

export class DetailField {
    Column: any;
    FieldDescription: string;
    FieldName: string;
    Group: any;
    HideFooter: boolean;
    HideHeader: boolean;
    HideFilter: boolean;
    LookupGridUrl: string;
    LookupObjectID: number;
    LookupObjectType: string;
    LookupFieldTypeID: number;
    LookupType: number;
    MultipleValues: any;
    Name: string;
    Row: any;
    ScriptProperty: any;
    TooltipContext: any;
    TooltipID: any;
    TooltipType: any;
    TooltipUrl: string;
    Value: string;
    Values: DetailSubField[];
    Type: DetailFieldType = DetailFieldType.Field;
    Data: any;
    DataType: string;
    ShowIfEmpty: boolean;
}

export class Synonym {
    Predicate: string;
    CustomID: number;
    IntersectID: number;
    IntersectMapID: number;
    Name: string;
    Description: string;
    Object: string;
    ObjectID: number;
    ObjectTypeName: string;
    Url: string;
    SubjectArea: string;
    TaxonomyTypeID: number;
    ParentName: string;
    ParentID: number;
    ParentUrl: string;
}

export class SynonymItem {
    ID: string;
    Name: string;
    TargetingSubject: boolean;
}

export class SynonymEditorModel {
    typeIsSubject: boolean;
    items: SynonymItem[];
}

export class SynonymEditModel {
    Type: string;
    ID: number;
    Synonym: string;
    TypeIsSubject: boolean;
    PredicateID: number;
}

export class NymType {
    Enabled: boolean;
    ID: number;
    Name: string;    
}

export class AttributeHeirarchyItem {

    Items: AttributeHeirarchyItem[];
    ID: string;
    ParentID: string;
    TypeID: number;
    ObjectTypeName: string;
    ObjectType: string;
    ObjectID: number;
    ParentObjectType: string;
    ParentObjectID: number;
    TargetObjectType: string
    TargetObjectID: number;
    Name: string;
    AttributeTypeCategory: string;
    ShowNameInTree: boolean;
    IsTechnical: boolean;
    IsCategory: boolean = false;
    expanded: boolean = true;

    UID: string;
    ParentUID: string;
}


export class ToolbarItem {
    Context: string;
    Icon: string;
    Title: string;
    Description: string;
    Items: ToolbarItem[];
    Uri: string;
}

export class ToolbarItemNg {
    Icon: string;
    Title: string;
    Description: string;
    Action: string;
    Params: any;
    Items: ToolbarItemNg[];
}

export class ObjectDetail {
    ID: number;
    Name: string;
    DisplayValue: string;
    TextPath: string;
    Description: string;
    ParentID: number;
    ParentType: string;
    Url: string;
    TypeID: any;
    Type: string;
    TypeName: string;
    IconBackColor: string;
    IconForeColor: string;
    IconText: string;
    AssetID: number;
    AssetTypeUid: string;
}


export class ObjectAction {
    Name: string;
    Value: boolean;
}

export class Classification {
    ID: number;
    Name: string;
}