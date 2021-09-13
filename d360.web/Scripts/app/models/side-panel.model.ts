import { PopupMenuItem } from "../components/shared/controls/popup-menu/popup-menu.component";

type WithOptional<T, K extends keyof T> = Omit<T, K> & Partial<T>

export class SidePanelButton {
    label: string;
    tooltip: string;
    disabledTooltip: string;
    nothingSelectedMessage: string;
    notApplicableMessage: string;
    key: string;
    icon: string;
    disabled: boolean = false;
    visible: boolean = true;
    needsSelection: boolean = true;
    panelMenu: PopupMenuItem[] = [];

    constructor(data: WithOptional<SidePanelButton, "disabled" | "visible" | "needsSelection" | "panelMenu">) {
        Object.assign(this, data);
    }
}