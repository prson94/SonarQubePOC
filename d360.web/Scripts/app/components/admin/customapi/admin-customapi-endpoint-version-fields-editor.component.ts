import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiField } from '../../../models/custom-api.model';
import { FieldType } from '../../../models/fields.model';
import { CustomAPIService } from '../../../services/custom-api.service';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-api-endpoint-version-fields-editor',
    providers: [CustomAPIService],
    templateUrl: './admin-customapi-endpoint-version-fields-editor.component.html'
})

export class AdminCustomAPIEndpointVersionFieldsEditorComponent extends BaseComponent implements OnInit {
    @Input() model: ApiField;
    @Input() versionId: number;
    @Input() entityId: number;
    @Output() onSaveClick = new EventEmitter();
    @Output() onCloseClick = new EventEmitter();

    isAdding: boolean = false;

    labelSave = $localize`Save`;
    labelClose = $localize`Close`;

    private fieldTypes: FieldType[] = [];
    private multiSelectFieldTypes: FieldType[] = [];
    private selectedFields: any[] = [];
    private showMultiselectOptions: boolean = false;

    constructor(
        protected customAPIService: CustomAPIService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super(settingsService);
    }

    ngOnInit(): void {
        this.load();
    }

    private load(): void {
        if (this.model == null) {
            this.model = new ApiField();
            this.model.EntityID = this.entityId;
            this.isAdding = true;
            this.isLoading = true;

            this.customAPIService.getEndpointVersionField_FieldTypes(this.versionId).subscribe(
                r => {
                    this.fieldTypes = r;
                    this.isLoading = false;
                    if (this.fieldTypes != null && this.fieldTypes.length > 0) {
                        this.customAPIService.getEndpointVersionField_LookupFieldTypes(this.fieldTypes[0].ID).subscribe(t => {
                            this.multiSelectFieldTypes = t;
                        }
                        );
                    }
                }
            );
        } else {
            this.customAPIService.getEndpointVersionFieldEditorModel(this.model.ID).subscribe(
                r => {
                    this.fieldTypes = r.fieldTypes;
                    this.multiSelectFieldTypes = r.multiSelectFieldTypes;
                    this.model = r.model;
                    this.changeFieldType(this.model.FieldTypeID);
                    this.selectedFields = r.selectedFields;
                }
            );
        }
    }

    private changeFieldType(e: any) {
        let field = this.fieldTypes.find(f => f.ID == e);

        this.model.FieldTypeID = e;
        this.model.MultiSelectFields = [];
        this.selectedFields = [];
        this.showMultiselectOptions = field && field.AllowMultipleValues && field.Type.toLowerCase() == "lookup";

        if (e != null && e > 0)
            this.customAPIService.getEndpointVersionField_LookupFieldTypes(e).subscribe(
                t => {
                    this.multiSelectFieldTypes = t;
                }
            );
    }

    private save() {
        this.isLoading = true;
        if (this.showMultiselectOptions) {
            let multiSelectFields = [];
            this.selectedFields.forEach(s => {
                multiSelectFields.push({
                    EntityFieldTypeID: this.model.ID,
                    FieldTypeID: s.Value
                });
            });

            this.model.MultiSelectFields = multiSelectFields;
        } else {
            this.model.MultiSelectFields = [];
            this.model.ItemNameOverride = null;
        }

        this.customAPIService.saveEndpointVersionField(this.model).subscribe(
            r => {
                this.onSaveClick.emit(this.model);

                this.isLoading = false;
            }
        );
    }

    private close() {
        this.onCloseClick.emit();
    }

    private valid(): boolean {
        return !(this.model.FieldTypeID == null || this.model.FieldTypeID < 1);
    }

    get headerTitle(): string {
        if (this.isAdding) {
            return $localize`Add Version Field`;
        }
        return $localize`Edit Version Field`;
    }
}
