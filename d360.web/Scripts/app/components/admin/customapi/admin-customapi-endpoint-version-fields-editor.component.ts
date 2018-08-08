import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SurveysService } from '../../../services/surveys.service';
import { MessagesService } from '../../../services/messages.service';
import { BaseComponent } from '../../shared/base.component';
import { CustomAPIService } from '../../../services/custom-api.service';
import { ApiService, ApiEndpoint, ApiVersion, ApiField } from '../../../models/custom-api.model';
import { Router, ActivatedRoute } from '@angular/router';
import { FieldType } from '../../../models/fields.model';

@Component({
    selector: 'd3s-admin-api-endpoint-version-fields-editor',
    providers: [CustomAPIService],
    template: `              
                    <header>{{ isAdding ? "Add" : "Edit" }} Version Field</header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div *ngIf="!isLoading">
                        <div class="row" style="padding-top: 10px">
                            <div class="col s6">
                                <div class="FieldName">
                                    Field
                                </div>
                                <div>
                                    <select [ngModel]="model.FieldTypeID" (ngModelChange)="changeFieldType($event)" style="width: 95%">
                                        <option></option>
                                        <option *ngFor="let f of fieldTypes" [value]="f.ID">{{f.FriendlyName}}</option>
                                    </select>
                                </div>
                            </div>
                        </div>   
                        <div class="row" style="padding-top: 10px">
                            <div class="col s6">
                                <div class="FieldName">
                                    Xml Field Override Name
                                </div>
                                <div>
                                    <input type="text" style="width: 95%"  [(ngModel)]="model.XmlFieldNameOverride"/>
                                </div>
                            </div>
                            <div class="col s6">
                                <div class="FieldName">
                                    Json Field Override Name
                                </div>
                                <div>
                                    <input type="text" style="width: 95%"  [(ngModel)]="model.JsonFieldNameOverride"/>
                                </div>
                            </div>
                        </div>    
                        <div class="row" style="padding-top: 10px">
                            <div class="col s4">
                                <input type="checkbox" [(ngModel)]="model.AllowSelect" /> Allow Select
                            </div>
                            <div class="col s4">
                                <input type="checkbox" [(ngModel)]="model.AllowFilter" /> Allow Filter
                            </div>
                            <div class="col s4">
                                <input type="checkbox" [(ngModel)]="model.AllowSort" /> Allow Sort
                            </div>
                        </div>  
                        <div class="row" *ngIf="showMultiselectOptions" style="padding-top: 10px">
                            <div class="col s6">
                                <div class="FieldName">Item Name Override</div>
                                <div>
                                    <input type="text" [(ngModel)]="model.ItemNameOverride" style="width:95%" />
                                </div>
                            </div>
                            <div class="col s6" *ngIf="multiSelectFieldTypes != null && multiSelectFieldTypes.length > 0">
                                <div class="FieldName">Multiselect Fields</div>
                                <div>
                                     <p-multiSelect [options]="multiSelectFieldTypes" [(ngModel)]="selectedFields" optionLabel="Text" dataKey="Value">

                                     </p-multiSelect>
                                </div>
                            </div>
                        </div>
                        <div class="row" style="padding-top: 10px">
                            <div class="col s12">
                                <button pButton label="Save" type="button" (click)="save()" [disabled]="!valid()"></button>
                                <button pButton label="Close" type="button" (click)="close()"></button>
                            </div>
                        </div>
                    </div>
                `
})

export class AdminCustomAPIEndpointVersionFieldsEditorComponent extends BaseComponent implements OnInit {
    @Input() model: ApiField;
    @Input() versionId: number;
    @Input() entityId: number;
    @Output() onSaveClick = new EventEmitter();
    @Output() onCloseClick = new EventEmitter();

    private isAdding: boolean = false;
    private fieldTypes: FieldType[] = []; 
    private multiSelectFieldTypes: FieldType[] = [];
    private selectedFields: any[] = [];
    private showMultiselectOptions: boolean = false;

    constructor(
        protected customAPIService: CustomAPIService,
        protected messagesService: MessagesService,
        private route: ActivatedRoute,
        private router: Router,
    ) {
        super();
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
            this.customAPIService.getEndpointVersionField_FieldTypes(this.versionId)
                .then(r => {
                    this.fieldTypes = r;
                    this.isLoading = false;
                    if (this.fieldTypes != null && this.fieldTypes.length > 0) {
                        this.customAPIService.getEndpointVersionField_LookupFieldTypes(this.fieldTypes[0].ID)
                            .then(t => {
                                this.multiSelectFieldTypes = t;
                            });
                    }
                });

        } else {
            this.customAPIService.getEndpointVersionFieldEditorModel(this.model.ID)
                .then(r => {
                    this.fieldTypes = r.fieldTypes;
                    this.multiSelectFieldTypes = r.multiSelectFieldTypes;
                    this.model = r.model;
                    this.changeFieldType(this.model.FieldTypeID);
                    this.selectedFields = r.selectedFields;
                    //console.log('loaded edit', r);
                });
        }

    }

    private changeFieldType(e: any) {
        this.model.FieldTypeID = e;
        this.model.MultiSelectFields = [];
        this.selectedFields = [];

        let field = this.fieldTypes.find(f => f.ID == e);
        if (field && field.AllowMultipleValues && field.Type.toLowerCase() == "lookup") {
            this.showMultiselectOptions = true;
        } else {
            this.showMultiselectOptions = false;
        }
        if (e != null && e > 0)
            this.customAPIService.getEndpointVersionField_LookupFieldTypes(e)
                .then(t => {
                    this.multiSelectFieldTypes = t;
                });
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

        this.customAPIService.saveEndpointVersionField(this.model)
            .then(r => {
                this.onSaveClick.emit(this.model);
                this.isLoading = false;
            });
    }

    private close() {
        this.onCloseClick.emit();
    }

    private valid(): boolean {
        if (this.model.FieldTypeID == null || this.model.FieldTypeID < 1)
            return false;

        return true;
    }
}