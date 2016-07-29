export class Breadcrumb {
    text: string = "-";
    link: string = null;
    active: boolean;
    objectType: string;
    objectId: number;

    constructor(text?: string, link?: string, active?: boolean, type?: string, objectId?: number) {
        this.text = text === undefined ? "-" : text;
        this.link = link === undefined ? null : link;
        this.active = active === undefined ? false : active;
        this.objectType = type === undefined ? undefined : type;
        this.objectId = objectId === undefined ? undefined : objectId;
    }

    public hasLink(): boolean {
        return (this.link && this.link.length > 0 && !this.active);
    }
}

export class BreadcrumbItem {
    Name: string;
    Url: string;
    Active: boolean;
}