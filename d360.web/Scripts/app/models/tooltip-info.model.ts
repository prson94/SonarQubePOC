import { Field, FieldType } from './fields.model';
import { ObjectDetail } from './object-detail.model';

export class TooltipFieldValue {
    Name: string;
    Value: string;
}

export class TooltipInfo {
    DisplayName: string;   
    TypeName: string;
    Url: string;    
    FieldValues: TooltipFieldValue[];    
}

export class LookupTooltipInfo {
    html: string;
}