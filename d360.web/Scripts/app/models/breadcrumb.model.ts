export class Breadcrumb {
    text: string = "-";
    link: string = null;

    constructor(text?: string, link?: string) {
        this.text = text === undefined ? "-" : text;
        this.link = link === undefined ? null : link;
    }
}