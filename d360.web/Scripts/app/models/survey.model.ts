import { DropdownOption } from './dropdown.model';

export enum SurveyTypeDisplayStyle {
    RadioList = 1,
    Rating = 2,
    CheckList = 3,
}

export class SurveyType {
    Name: string;
    Description: string;
    ID: number;
    Object: string;
    ObjectID: number;
    ValidForDays: number;
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
    Desciption: string;
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