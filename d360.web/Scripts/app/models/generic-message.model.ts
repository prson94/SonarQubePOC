export class GenericMessageModel {
    uid: string;
    messageType: GenericMessageType;
    data: any;
}

export enum GenericMessageType {
    Tags = 0,
    Generic = 1
}
