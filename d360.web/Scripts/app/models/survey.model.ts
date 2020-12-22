import { DropdownOption } from './dropdown.model';

export enum SurveyTypeDisplayStyle {
    RadioList = 1,
    Rating = 2,
    CheckList = 3,
}
export class Survey {
    Name: string;
    SurveyTypeUid: string;
}
export class SurveyType {
    //legacy IDs for admin section
    ID: number;
    Object: string;
    ObjectID: number;

    Name: string;
    Description: string;
    Uid: string;
    AssetTypeUid: string;
    ValidForDays: number;
}

export class SurveyTypeDetails {
    Uid: string;
    AssetTypeUid: string;
    Name: string;
    Description: string;
    ValidForDays: number;
    Questions: Question[];

}
export class Question {
    Uid: string;
    Name: string;
    DisplayStyle: SurveyTypeDisplayStyle;
    Comments: string;
    Options: Option[];
    Value: any;
}

export class Option {
    Name: string;
    Value: number;
    IsChecked: boolean = false;
}

export class SurveyQuestionType {
    ID: number;
    Name: string;
    DisplayStyle: string;
    OptionCount: number;
    Description: string;
}

export class SurveyQuestionOption {
    ID: number;
    Name: string;
    Value: number;
    IsChecked: boolean;
}

export class SurveyQuestionTypeDetails {
    Description: string;
    DisplayStyle: SurveyTypeDisplayStyle;
    DisplayStyleOptions: DropdownOption[];
    ID: number;
    Name: string;
    SurveyTypeID: number;
    Items: SurveyQuestionOption[];
    Values: SurveyQuestionOption[]; //used on response
    Comments: string;
}

export class SurveyResponse {
    Questions: SurveyQuestionTypeDetails[];
}

export class SurveyQuestionResponseApiModel {
    Responses: number[];
    SurveyQuestionUid: string;
    Comments: string;
}

export class SurveyResultsApiModel {
    AssetUid: string;
    Questions: SurveyQuestionResponseApiModel[] = [];
}