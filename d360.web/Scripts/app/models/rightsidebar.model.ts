import { Subject } from "rxjs";

export class RightSidebarItem {
    constructor(title?: string, tag?: any, icons?: string[], url?: string, count?: number, priority?: number) {
        if (title) this.title = title;
        if (tag) this.tag = tag;
        this.active = false;
        this.icons = icons ? icons : ["fa-share-alt"];
        if (url != undefined) this.url = url;
        if (count != undefined) this.count = count;
        if (priority != undefined) this.orderPriority = priority;
    }
    title: string;
    tag: any;
    active: boolean;
    icons: string[];
    url: string;
    hasDynamicUrl: boolean;
    dynamicUrlCallback: Function;
    count: number;
    orderPriority: number = 99;
}

export class DynamicButton {
    constructor(text: string) {
        this.text = text;
    }
   
    text: string;
    disabled: boolean = false;
    isLoading: boolean = false;
    dynamicCallback: Function;
}

export class AssetAction {
    isVisible: boolean = false;
    showBack: boolean;
    showEdit: boolean;
    showDelete: boolean;

    editCallback: Function;


    edit: EditFormData;
}

export class EditFormData {
    objectID: string;
    objectType: string;
    selected: any;
    title: string;
    saveClick: Function;
    showAsModal: boolean;
    modalTitle: string;
    isModalVisible: boolean;
    closeClick: Function;
}