import { DropdownOption } from './dropdown.model';

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
}

export class SurveyQuestionTypeDetails {
    Desciption: string;
    DisplayStyle: number;
    DisplayStyleOptions: DropdownOption[];
    ID: number;
    Name: string;
    SurveyTypeID: number;
    Items: SurveyQuestionOption[];
}

export class SurveyResponse {
    Comments: string;
}