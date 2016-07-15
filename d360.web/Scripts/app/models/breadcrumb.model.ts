export class Breadcrumb {
    text: string = "-";
    link: string = null;
    active: boolean;

    constructor(text?: string, link?: string, active?: boolean) {
        this.text = text === undefined ? "-" : text;
        this.link = link === undefined ? null : link;
        this.active = active === undefined ? false : active;
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