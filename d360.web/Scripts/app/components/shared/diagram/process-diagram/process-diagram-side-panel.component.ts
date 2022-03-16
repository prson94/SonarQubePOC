
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output, OnDestroy } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { AssetTypeService } from '../../../../services/asset-type.service';
import { EditorField } from '../../../../models/editor-field.model';
import { CompanySettingsService } from '../../../../services/settings.service';
import { LinkClickInterceptor } from '../../../../services/href-click-service';
import { Subscription } from 'rxjs';
@Component({
    selector: 'd3s-process-diagram-side-panel',
    templateUrl: './process-diagram-side-panel.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramSidePanelComponent extends DiagramBaseComponent implements OnChanges, OnDestroy {
    @Input() nodeData: any;
    @Input() linkData: any;
    @Input() isReadOnly: boolean = true;
    @Input() disallowedNames: string[] = [];
    @Input() viewType: string = 'diagram';
    @Input() assetUid: string = '';

    @Output() nodeDataChange = new EventEmitter();
    @Output() nodeDeselected = new EventEmitter();
    @Output() linkDataChange = new EventEmitter();

    private assetName: string = '';

    hrefSub: Subscription;
    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;
    loadedNodes: any[] = [];

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        private assetTypeService: AssetTypeService,
        protected settingsService: CompanySettingsService,
        private linkClickInterceptor: LinkClickInterceptor
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.nodeDeselected.emit(null);
            this.linkClickInterceptor.handleEvent(this, ev);
        });

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.nodeData && changes.nodeData.currentValue != changes.nodeData.previousValue) {
            if (this.nodeData) {
                this.assetName = this.nodeData['Name'];
                this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;

                var exists = this.loadedNodes.filter((node) => node.key === this.nodeData.key);
                if (exists.length === 0) {
                    this.loadedNodes.push(this.nodeData);
                }
            }
        }
    }

    ngOnDestroy() {
        if (this.hrefSub) {
            this.hrefSub.unsubscribe();
        }
        if (this.sidebarSubscription) {
            this.sidebarSubscription.unsubscribe();
        }
    }

    //process dynamiceditor onSubmit() form data
    //check for missing fields and set value to ''
    //ignore system fields (Uid/AssetTypeUid)
    private onModelChange($event) {
        if (this.isReadOnly && !this.nodeData) {
            return;
        }

        var data = $event['values'];
        data.key = this.nodeData.key;

        if (data && data['Name']) {
            this.assetName = data['Name'];
        }

        for (var prop in data) {
            if (data[prop] == undefined) {
                delete data[prop];
            }
            if (prop == 'Uid' || prop == 'AssetTypeUid') {
                delete data[prop];
            }
        }
        var fields = $event['fields'] as EditorField[];
        fields.filter((x) => x.FieldTypeID).forEach((f) => {
            if (data[f.FieldName] == undefined) {
                data[f.FieldName] = '';
            }
            else {
                if (f.FieldType == 'DateTime') {
                    var dateTime = new Date(data[f.FieldName]);
                    dateTime.setMinutes(dateTime.getMinutes() - dateTime.getTimezoneOffset());
                    data[f.FieldName] = dateTime.toISOString();
                }
            }
        });
        this.nodeDataChange.emit(data);
    }

    public pad(s): string { return (s < 10) ? '0' + s : s; }

    showEmptyOverlay() {
        var selectedNodeData = this.nodeData || this.selectedAsset || this.selectedReferenceItem || this.selectedTag;
        return !selectedNodeData && (!this.linkData || this.isReadOnly);
    }
}