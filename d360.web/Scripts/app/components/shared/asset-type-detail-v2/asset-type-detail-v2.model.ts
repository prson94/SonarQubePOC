export interface AssetTypeDetailField {
    name: string;
    value: any;
    type: AssetTypeDetailFieldType;
}

export interface AssetTypeDetailCategory {
    active: boolean;
    fields: AssetTypeDetailField[];
    name: string;
}

export interface ControlsOptions {
    showEdit: boolean;
    showOpen: OpenBehaviour;
}

export enum OpenBehaviour {
    CURRENT_TAB,
    NEW_TAB
}

export enum AssetTypeDetailFieldType {
    TEXT = "TEXT",
    BOOL = "BOOLEAN",
    COLOR = "COLOR",
    ICON = "ICON",
    SYSTEM = "SYSTEM",
    FLOW_OBJECT_TYPE = "FLOW_OBJECT_TYPE",
    HTML = "HTML"
}