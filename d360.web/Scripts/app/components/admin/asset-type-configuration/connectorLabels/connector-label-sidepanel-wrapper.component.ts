import { Component, Input } from "@angular/core";
import { IOutputData } from "angular-split";
import { SidePanelService } from "../../../../services/side-panel.service";


@Component({
    selector: "d3s-connector-label-sidepanel-wrapper",
	templateUrl: './connector-label-sidepanel-wrapper.component.html',
	styleUrls: ['./connector-label-sidepanel-wrapper.component.less']
})
export class ConnectorLabelSidePanelWrapperComponent {
	@Input() sidePanelStorageKey: string;
	@Input() selectedItem: string;

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
