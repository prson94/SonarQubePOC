export class RightSidebarItem {
    constructor(title?: string, tag?: any, icons?: string[], url?: string) {
        if (title) this.title = title;
        if (tag) this.tag = tag;
        this.active = false;
        this.icons = icons ? icons : ["fa-share-alt"];
        if(url!= undefined) this.url = url;
    }
    title: string;
    tag: any;
    active: boolean;
    icons: string[];
    url: string;
    hasDynamicUrl: boolean;
    dynamicUrlCallback: Function;
}
