export class GenericMessageModel {
    uid: string;
    messageType: GenericMessageType;
    text: string;
}

export enum GenericMessageType {
    Tags = 0,
    Generic = 1
}
