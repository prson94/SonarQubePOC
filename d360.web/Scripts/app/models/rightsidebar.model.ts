export class RightSidebarItem {
    constructor(title?: string, tag?: any, icons?: string[]) {
        if (title) this.title = title;
        if (tag) this.tag = tag;
        this.active = false;
        this.icons = icons ? icons : ["fa-share-alt"]; 
    }
    title: string;
    tag: any;
    active: boolean;
    icons: string[];
}
