export class Audit {
    action: string;
    actionDescription: string;
    actionObject: string;
    actionObjectID: number;
    actionObjectName: string;
    actionAssetTypeUid: string;
    actionObjectTypeName: string;
    date: Date;
    actionAssetUid: string;
    object: string;
    objectName: string;
    resourceName: string;
    field: string;
    class: number;
    newValue: string;
    previousValue: string;
    version: string;
    resourceUid: string
}

export class AuditResults {
    items: Audit[];
    total: number;
    pageSize: number;
    pageNum: number;
}
export class AuditApiFilters {
    _pageSize: number;
    _pageNum: number;
    _order: string;
    _direction: string;
    _filter: string;
}
export class AuditObject {
    ObjectId: number;
    Object: string;
    DisplayValue: string;
}
export class AuditFilterLists {
    resourceName: string[];
    action: string[];
    actionObject: string[];
}