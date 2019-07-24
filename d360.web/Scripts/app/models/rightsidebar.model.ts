import { Subject } from "rxjs";

export class RightSidebarItem {
    constructor(title?: string, tag?: any, icons?: string[], url?: string, count?: number) {
        if (title) this.title = title;
        if (tag) this.tag = tag;
        this.active = false;
        this.icons = icons ? icons : ["fa-share-alt"];
        if (url != undefined) this.url = url;
        if (count != undefined) this.count = count;

    }
    title: string;
    tag: any;
    active: boolean;
    icons: string[];
    url: string;
    hasDynamicUrl: boolean;
    dynamicUrlCallback: Function;
    count:number
}

export class DynamicButton {
    
    private subject = new Subject<boolean>();

    constructor(text: string) {
        this.text = text;
    }
    EmitFunction() {
        this.subject.next(true);
    }
    text: string;
    disabled: boolean = false;
    isLoading: boolean = false;
    obervable$ = this.subject.asObservable();
}