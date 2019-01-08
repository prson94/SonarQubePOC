export enum LinkTarget {
    NewWindow = 0,
    Self = 1,
    RouterLink = 2
}

export class Shortcut {
    ID: number;
    Name: string;
    Icon: string;
    IconUrl: string;
    Url: string;
    IconPayload: string;
    Description: string;
    IconColor: string;
    TitleColor: string;
    BackgroundColor: string;
    LinkTarget: LinkTarget;
    FullURL: string;
}