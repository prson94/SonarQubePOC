import { Field, FieldType } from './fields.model';
import { ObjectDetail } from './object-detail.model';
import { AssetTypeClass } from './asset.model';

export class TooltipFieldValue {
    Name: string;
    Value: string;
    Type: string;
}

export class TooltipFieldLevelPath {
    Path: string;
    LevelName: string;
    Url: string
    Level: string;
}

export class TooltipInfo {
    DisplayName: string;   
    TypeName: string;
    Url: string;    
    FieldValues: TooltipFieldValue[];
    Levels: TooltipFieldLevelPath[];
    Description: string;
    ShowTooltip: boolean;
    Class: AssetTypeClass;
    AssetID: number;
    UID: string;
    WorkflowTypeUID: string;
    WorkflowVersionUID: string;
}

export class LookupTooltipInfo {
    html: string;
}