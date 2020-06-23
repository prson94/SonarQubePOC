
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { AssetTypeService } from '../../../../services/asset-type.service';
@Component({
    selector: 'd3s-process-diagram-asset-editor',
    templateUrl: './process-diagram-asset-editor.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramAssedEditorComponent extends DiagramBaseComponent implements OnChanges {
    @Input() nodeData: any;
    @Output() nodeDataChange = new EventEmitter();

    public formGroupCached: any;


    private formGroupCache: any = {};


    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        private assetTypeService: AssetTypeService
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.nodeData && changes.nodeData.currentValue != changes.nodeData.previousValue) {
            if (this.nodeData)
                this.load();
        }
    }

    load() {
        this.formGroupCached = null;
        if (this.formGroupCache && this.formGroupCache[this.nodeData.key]) {
            this.formGroupCached = this.formGroupCache[this.nodeData.key];
        }
        this.cdRef.detectChanges();
        this.cdRef.markForCheck();
    }

    private onModelChange($event) {
        console.log($event);
        var data = $event['data'];
        data['key'] = this.nodeData.key;
        this.nodeDataChange.emit(data);

        this.formGroupCache[this.nodeData.key] = $event['formGroup'];
    }


}