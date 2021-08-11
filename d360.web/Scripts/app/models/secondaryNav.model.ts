import { Subject } from "rxjs";
import { AssetTypeClass } from "./asset.model";

export class SecondaryNavItem {
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
    count: number;
    orderPriority: number = 99;
    subTabsUrl: string[] = [];
    warningMessage: string = ''
}

export class NavState {
    currentObject: SecondaryNavCurrentObject;
    currentHome: string;
    shownTabs: Array<SecondaryNavItem> = [];
    currentTab: SecondaryNavItem;
    currentArea: any;
}

export class SecondaryNavState {
    private maxStack = 50;
    currentState: NavState = new NavState();
    previousStates: NavState[] = [];

    public pushPreviousState(item: NavState) {
        if (this.previousStates.length >= this.maxStack) {
            this.previousStates.splice(0, 1);
        }
        this.previousStates.push(item);
    }
}

export class SecondaryNavCurrentObject {
    constructor(objectType: string, objectTypeID: number, objectName: string, objectID: number, isType: boolean, hasWorkFlow?: boolean, Uid?: string, hasRequestCertificationWorkflow?: boolean) {
        this.objectType = objectType;
        this.objectTypeID = objectTypeID;
        this.objectName = objectName;
        this.objectID = objectID;
        this.isType = isType;
        this.hasWorkFlow = hasWorkFlow == undefined ? false : hasWorkFlow;
        this.hasRequestCertificationWorkflow = !hasRequestCertificationWorkflow ? false : hasRequestCertificationWorkflow;
        this.Uid = Uid == undefined ? undefined : Uid;
    }
    objectType;
    objectTypeID;
    objectName;
    objectID;
    isType;
    hasWorkFlow;    
    Uid;
    hasRequestCertificationWorkflow;
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
    type: string = '';
    isVisible: boolean = false;
    showBack: boolean;
    showEdit: boolean;
    showDelete: boolean;

    editCallback: Function;
    deleteCallback: Function;
    backCallback: Function;

    edit: EditFormData;
    delete: DeleteFormData;
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

export class DeleteFormData {
    callback: Function;
    item: any;
    showAsModal: boolean;
    modalTitle: string;
    isModalVisible: boolean;
}

export class SecondaryNavPostModel {
    ObjectId: number;
    ObjectType: string;
    AssetId: number;
    AssetUid: string;
    AssetTypeUid: string;
    PreloadData: boolean;
    Class: AssetTypeClass;
    DisplayValue: string;
}