export enum SurveyTypeDisplayStyle {
    RadioList = 1,
    Rating = 2,
    CheckList = 3,
}

export enum SurveyTypeDisplayStyleV2 {
    Radio = 'Radio',
    Rating = 'Rating',
    CheckList = 'CheckList',
}

export class Survey {
    Name: string;
    SurveyTypeUid: string;
}

export class SurveyTypeUpsertModel {
    Name: string;
    Description: string;
    Uid: string;
    AssetTypeUid: string;
    ValidForDays: number;
}

export interface QuestionTypeV2 {
    Uid: string;
    Name: string;
    Description: string;
    DisplayStyle: SurveyTypeDisplayStyleV2;
    Options: { Name: string, Value: number }[]
}

export interface SurveyTypeV2 {
    Uid: string;
    AssetTypeUid: string;
    Name: string;
    Description: string;
    ValidForDays: number;
    CreatedOn: string;
    CreatedByUid: string;
    UpdatedOn: string;
    UpdatedByUid: string;
    NumberOfResponses: number;
    Questions: QuestionTypeV2[]
}

export interface SurveyTypesResponse {
    pageSize: number;
    pageNum: number;
    total: number;
    items: SurveyTypeV2[]
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

export class SurveyQuestionResponseApiModel {
    Responses: number[];
    SurveyQuestionUid: string;
    Comments: string;
}

export class SurveyResultsApiModel {
    AssetUid: string;
    Questions: SurveyQuestionResponseApiModel[] = [];
}