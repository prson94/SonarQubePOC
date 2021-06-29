import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, Output, EventEmitter } from '@angular/core';
//import { DetailRow, DetailField, DetailFieldType, ComplexLookupType, NymType, Category } from '../../../models/object-detail.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetService } from '../../../services/asset.service';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'side-panel',
    templateUrl: './side-panel.component.html',
})

export class SidePanelComponent extends BaseComponent{
    @Input() dataProfile: any;

    private showDataProfile: boolean;
    private showSidePanel: boolean;    
    private activePanelName: string;

    constructor(
        private cdRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnInit() {
        this.showDataProfilePanel();
    }

    private showDataProfilePanel() {
        this.activePanelName = "Profiling";
        this.showDataProfile = true;    
        this.showSidePanel = true;
    }

    private hideSidePanel() {
        this.showDataProfile = false;
        this.showSidePanel = false;
    }
}