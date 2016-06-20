export interface IFieldsService {
    getFields(objectID: number, objectType: string): Promise<FieldDefinition[]>;
}

export class FieldDefinition {

    ObjectType: string;
    ObjectID: string;
    ID: string;
    Category: string;
    FriendlyName: string;
    SortOrder: string;
    IsRequired: boolean;
    IsListable: boolean;
    DisplayDescription: string;
    FormDescription: string;
}