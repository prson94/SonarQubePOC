import { Component, Input } from "@angular/core";
import { IOutputData } from "angular-split";
import { TreeNode } from "primeng/api";
import { SidePanelService } from "../../../../services/side-panel.service";


@Component({
    selector: "d3s-asset-type-list-sidepanel-wrapper",
    templateUrl: './asset-type-list-sidepanel-wrapper.component.html',
    styleUrls: ['./asset-type-list-sidepanel-wrapper.component.less']
})
export class AssetTypeListSidePanelWrapperComponent {
    @Input() sidePanelStorageKey: string;
    @Input() selectedItem: TreeNode;

    sidePanelOpen = false;
    
    constructor(public sidePanelService: SidePanelService) {
    }

    getSidePanelWidth(): number {
        return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
    }

    getSidePanelMaxWidth(): number {
        return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
    }

    getSidePanelMinWidth(): number {
        return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
    }

    onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
        this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
    }
}
