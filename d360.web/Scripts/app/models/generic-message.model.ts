export class GenericMessageModel {
    uid: string;
    assetUIDList: string[];
    messageType: GenericMessageType;
    data: any;
}

export enum GenericMessageType {
    Generic = 0,
    AddTag = 1,
    DeleteTag = 2
}
