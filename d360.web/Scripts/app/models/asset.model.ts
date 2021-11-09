import { TreeNode } from "primeng/api";


export enum State {
    Unknown = -1,
    PendingAdd = 0,
    Active = 1,
    PendingDelete = 2,
    Deleted = 3,
    InActive = 4
}

export class AssetEditorModel {
    Uid: string;
    ParentUid: string;
    Fields: any;
}

export class AssetDetail {
    AssetTypeID: number;
    AssetTypeName: string;
    CreatedOn: Date;
    DisplayValue: string;
    ID: number;
    Object: string;
    ObjectID: number;
    State: number;
    Type: string;
    TypeID: number;
    UpdatedOn: Date;
}

export class AssetTypeMetricModel {
    Uid: string;
    Name: string;
    Class: string
}

export class AssetTypeEditorModel {
    AssetType: AssetType;
    ParentUid: string;
    Predicates: any[];
    Tokens: any[];
    Parents: any[];
}

export enum AssetTypeClass {
    BusinessAsset = 1,
    Model = 2,
    Policy = 6,
    Rule = 7,
    TechnicalAsset = 8,
    Reference = 9,
    Organization = 10,
    User = 11,
    Group = 12,
    ReferenceItemType = 14,
    DiagramAsset = 15,
}

export enum FlowObjectType {
    Event = 1,
    Activity = 2,
    Gateway = 3
}

export class AssetType {
    Uid: string;
    Name: string;
    Class: AssetTypeClass;
    FlowObjectType: FlowObjectType;
    Description: string;
    AutoDisplayDescription: boolean;
    DisplayFormat: string;
    ParentUid: string;
    Notes: string;
    UseAsTransformation: boolean;
    IconStyle: IconStyle = new IconStyle();
    Hierarchy: Hierarchy = new Hierarchy();
    AutoDisplayParent: boolean;
    CanEditParent: boolean;
}

export class AssetTypeClassApiModel {
    ID: number;
    Name: string;
    Value: AssetTypeClass;
    Description: string;
}

export class AssetTypeLevelApiModel {
    Level: number;
    Name: string;
    Description: string;
}

export class AssetTypeApiModel {
    uid: string;
    Name: string;
    Path: string;
    Class: AssetTypeClassApiModel;
    Description: string;
    AutoDisplayDescription: boolean;
    DisplayFormat: string;
    ParentUid: string;
    Notes: string;
    UseAsTransformation: boolean;
    Hierarchical: boolean;
    HierarchyMaximumDepth: number;
    FlowObjectType: FlowObjectType;
    ID: number;
    AssetTypeID: number;
    count: number = 0; //not currently loaded from API.

    Levels: AssetTypeLevelApiModel[];
}

export class IconStyle {
    ForeColor: string;
    BackColor: string;
    Icon: string;
}

export class Hierarchy {
    MaximumDepth: number;
    PredicateUid: string;
}

export class AssetCount {
    uid: string;
    parentUid: string;
    class: string;
    name: string;
    description: string;
    count: number;

    public static ConvertToTreeNode(data: AssetCount): TreeNode {
        let node: TreeNode = {};
        node.data = data;
        node.key = data.uid;
        node['id'] = data.uid;
        node['parentid'] = data.parentUid;
        return node;
    }


    public static ListToTree(arr: TreeNode[]): TreeNode[] {
        var tree = [],
            mappedArr = {},
            arrElem,
            mappedElem;

        // First map the nodes of the array to an object -> create a hash table.
        for (var i = 0, len = arr.length; i < len; i++) {
            arrElem = arr[i];
            mappedArr[arrElem.id] = arrElem;
            mappedArr[arrElem.id]['children'] = [];
        }

        for (var id in mappedArr) {
            if (mappedArr.hasOwnProperty(id)) {
                mappedElem = mappedArr[id];
                // If the element is not at the root level, add it to its parent array of children.
                if (mappedElem.parentid) {
                    if (mappedArr[mappedElem['parentid']]['children']) {
                        mappedArr[mappedElem['parentid']]['children'].push(mappedElem);
                    }
                }
                // If the element is at the root level, add it to first level elements array.
                else {
                    tree.push(mappedElem);
                }
            }
        }
        return tree;
    }
}