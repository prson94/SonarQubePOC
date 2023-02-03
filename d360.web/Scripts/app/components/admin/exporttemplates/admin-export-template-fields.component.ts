import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ExportTemplate } from '../../../models/export-template.model';
import { FieldsObservableService } from '../../../services/fieldsObservable.service';
import { FieldDefinition } from '../../../models/fields.model';
import { clone } from "lodash-es";
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-export-template-fields-component',
    templateUrl: 'admin-export-template-fields.component.html',
    providers: [FieldsObservableService],
})

export class AdminExportTemplateFieldsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() exportTemplate: ExportTemplate;
    @Output() saveFieldsClick = new EventEmitter();

    public availableFields: FieldDefinition[] = new Array<FieldDefinition>();
    public selectedFields: FieldDefinition[] = new Array<FieldDefinition>();

    labelSave = $localize`Save`;
    labelRevert = $localize`Revert Changes`;

    constructor(
        protected fieldsService: FieldsObservableService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.load();
    }

    private load() {
        this.availableFields = [];

        //load available fields for the artifact type
        this.fieldsService.getAssetTypeFields(this.exportTemplate.AssetTypeUID).subscribe(
            (data) => {
                data = data.filter((x) => x.Type !== 'ComplexRelationLookup' && x.Type !== 'OwnershipLookup' && x.Type !== 'JSON' && x.Type !== 'JsonElement');
                //split the string of selected fields and populate the selected fields array
                this.availableFields = this.setInitialFields(data);
            }
        );
    }

    public reset() {
        this.availableFields = [];
        this.load();
    }

    public setInitialFields(available): FieldDefinition[] {
        //reset the template fields back to original state
        let order: number = 0;
        this.selectedFields = [];        

        if (this.exportTemplate.IncludeFieldTypes) {
            const selectedFieldNames = this.exportTemplate.IncludeFieldTypes;
            for (let j = 0; j < selectedFieldNames.length; j++) {
                for (let k = 0; k < available.length; k++) {
                    if (selectedFieldNames[j] === available[k].Name) {
                        this.selectedFields[this.selectedFields.length] = available[k];
                        available[k].ExtOrder = order++;
                    }
                }
            }
        }

        for (let i = 0; i < available.length; i++) {
            if (available[i].ExtOrder == null)
                {available[i].ExtOrder = order++;}
        }
        return available;
    }

    public save() {
        const fields = "";
        let fieldTypes = [];
        fieldTypes = this.selectedFields.sort((a, b) => a.ExtOrder - b.ExtOrder).map((a) => a.Name);

        //trigger save event
        this.saveFieldsClick.emit({ IncludeFieldTypes: fieldTypes });
    }

    public top(event, field: FieldDefinition) {
        event.stopPropagation();
        this.isLoading = true;
        //push everything down        
        field.ExtOrder = 0;
        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID !== field.ID)
                {this.availableFields[i].ExtOrder++;}
        }
        this.availableFields = clone(this.availableFields);
        this.isLoading = false;
    }

    public bottom(event, field: FieldDefinition) {
        event.stopPropagation();
        //push everything up below this item
        this.isLoading = true;
        let found: boolean = false;
        let max: number = 0;
        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID === field.ID)
                {found = true;}
            if (found) {this.availableFields[i].ExtOrder++;}
            max = this.availableFields[i].ExtOrder;
        }
        field.ExtOrder = max + 1;
        this.availableFields = clone(this.availableFields);
        this.isLoading = false;
    }

    public up(event, field: FieldDefinition) {
        event.stopPropagation();
        this.isLoading = true;

        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID === field.ID && i > 0) {
                const order = this.availableFields[i].ExtOrder;
                this.availableFields[i].ExtOrder = this.availableFields[i - 1].ExtOrder;
                this.availableFields[i - 1].ExtOrder = order;

            }
        }
        this.availableFields = clone(this.availableFields);
        this.isLoading = false;
    }

    public down(event, field: FieldDefinition) {
        event.stopPropagation();
        this.isLoading = true;

        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID === field.ID && i < this.availableFields.length - 1) {
                const order = this.availableFields[i].ExtOrder;
                this.availableFields[i].ExtOrder = this.availableFields[i + 1].ExtOrder;
                this.availableFields[i + 1].ExtOrder = order;
            }
        }
        this.availableFields = clone(this.availableFields);
        this.isLoading = false;
    }
}