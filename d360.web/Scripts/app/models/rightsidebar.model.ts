export class RightSidebarItem {
    constructor(title?: string, tag?: any) {
        if (title) this.title = title;
        if (tag) this.tag = tag;
    }
    title: string;
    tag: any;
}
