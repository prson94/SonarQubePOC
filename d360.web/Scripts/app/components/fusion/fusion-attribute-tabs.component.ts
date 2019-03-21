import { Component, OnChanges, SimpleChanges, Input, Output, EventEmitter } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { StateService } from "../../services/state.service";



@Component({
    selector: 'd3s-fusion-attribute-tabs',
    templateUrl: './fusion-attribute-tabs.component.html'
})
export class FusionAttributeTabsComponent extends BaseComponent {

    activeIndex: number = this.stateService.fusionState.activeTabIndex;
    @Input() fusionId: number;

    @Input() selectedFusionAttributeTypeId: number;
    @Input() selectedFusionAttribute: any;
    @Input() initialFusionAttributeId: number;

    @Input() selectedFusionQueryAttributeTypeId: number;
    @Input() selectedFusionQueryAttribute: any;
    @Input() initialFusionQueryAttributeId: number;
    
    constructor(private stateService: StateService) {
        super();
    }

    tabs: any[] = [
        { key: 'ASSETS', loaded: false },
        { key: 'DATAPROFILING', loaded: false },
    ];

    activeTab:any = this.tabs[this.activeIndex];

    tabClick(e: any) {
        this.activeIndex = e.index;
        this.activeTab = this.tabs[e.index];
        this.stateService.fusionState.activeTabIndex = this.activeIndex;
        this.activeTab.loaded = true;
    }




    tabIsActive(key: string):boolean {
        return this.activeTab.key == key;
    }

}