
import { Component, Input, OnInit, ChangeDetectionStrategy, AfterViewChecked, OnChanges, SimpleChange, SimpleChanges, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { AssetTypeService } from '../../../../services/asset-type.service';
import { EditorField } from '../../../../models/editor-field.model';
import { CompanySettingsService } from '../../../../services/settings.service';
@Component({
    selector: 'd3s-process-diagram-asset-editor',
    templateUrl: './process-diagram-asset-editor.component.html',
    providers: [AssetTypeService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramAssetEditorComponent extends DiagramBaseComponent implements OnChanges {
    @Input() nodeData: any;
    @Input() isReadOnly: boolean = true;
    @Input() disallowedNames: string[] = [];
    @Output() nodeDataChange = new EventEmitter();
    private assetName: string = '';

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        private cdRef: ChangeDetectorRef,
        private assetTypeService: AssetTypeService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.nodeData && changes.nodeData.currentValue != changes.nodeData.previousValue) {
            if (this.nodeData) {
                this.assetName = this.nodeData['Name'];
            }
        }
    }

    //process dynamiceditor onSubmit() form data
    //check for missing fields and set value to ''
    //ignore system fields (Uid/AssetTypeUid)
    private onModelChange($event) {
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

}