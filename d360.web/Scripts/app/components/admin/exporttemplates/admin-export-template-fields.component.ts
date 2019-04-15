import { Component, OnDestroy, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { MessagesService } from '../../../services/messages.service';
import { Title } from '@angular/platform-browser';
import { ExportTemplateService } from '../../../services/export-template.service';
import { ExportTemplate } from '../../../models/export-template.model';
import { FieldsService } from '../../../services/fields.service';
import { FieldDefinition } from '../../../models/fields.model';
import * as _ from 'lodash';
import { forEach } from '@angular/router/src/utils/collection';

@Component({
    selector: 'd3s-admin-export-template-fields-component',
    template: ` 
                
                    <header>Fields</header>
                    <div class="row">
                        <div class="col s12">
                            <p-table #dt [scrollable]="true" sortField="ExtOrder" [sortOrder]="1"  scrollHeight="400px" [loading]="isLoading" loadingIcon="fa fa-spinner" [value]="availableFields" selectionMode="multiple" [globalFilterFields]="['Name']" [paginator]="false" [(selection)]="selectedFields">
                                <ng-template pTemplate="header">
                                    <tr>
                                        <th style="width: 30px"><p-tableHeaderCheckbox></p-tableHeaderCheckbox></th>
                                        <th>Name</th>                                        
                                        <th style="width: 30px"></th>
                                        <th style="width: 30px"></th>
                                        <th style="width: 30px"></th>
                                        <th style="width: 30px"></th>
                                    </tr>                                    
                                </ng-template>
                                <ng-template pTemplate="body" let-item>
                                    <tr [pSelectableRow]="item">
                                        <td  style="width: 30px"><p-tableCheckbox [value]="item"></p-tableCheckbox></td>
                                        <td>{{item.FriendlyName}}</td>        
                                        <td style="width: 30px">
                                            <div class="RowTools">
                                                <a (click)="top($event,item)" style="cursor:pointer;"><i class="fa fa-angle-double-up"></i></a>
                                            </div>
                                        </td>
                                        <td style="width: 30px">
                                            <div class="RowTools">
                                                <a (click)="up($event,item)" style="cursor:pointer;"><i class="fa fa-caret-up"></i></a>
                                            </div>
                                        </td>
                                        <td style="width: 30px">
                                            <div class="RowTools">
                                                <a (click)="down($event,item)" style="cursor:pointer;"><i class="fa fa-caret-down"></i></a>
                                            </div>
                                        </td>
                                        <td style="width: 30px">
                                            <div class="RowTools">
                                                <a (click)="bottom($event,item)" style="cursor:pointer;"><i class="fa fa-angle-double-down"></i></a>
                                            </div>
                                        </td>
                                    </tr>
                                </ng-template>                                
                            </p-table>                         
                        </div>                        
                    <div>             
                    <div class="row">
                        <div class="col s12">
                            <button pButton label="Save Changes" (click)="save()"></button>
                            <button pButton type="button" (click)="reset()" label="Revert Changes"></button>
                        </div>
                    </div>
                
                `,
    providers: [ExportTemplateService, FieldsService],
})

export class AdminExportTemplateFieldsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() exportTemplate: ExportTemplate;    
    @Output() saveFieldsClick = new EventEmitter();

    
    public availableFields: FieldDefinition[] = new Array<FieldDefinition>();
    
    public selectedFields: FieldDefinition[] = new Array<FieldDefinition>();
    

    constructor(        
        private exportTemplateService: ExportTemplateService,
        protected messagesService: MessagesService,
        protected fieldsService: FieldsService
    ) {
        super();                
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
            data => {
                data = data.filter(x => x.Type != 'Relation Lookup' && x.Type != 'Filtered Lookup' && x.Type != 'Ownership Lookup' && x.Type != 'Fusion Lookup')
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

        if (this.exportTemplate.IncludeFields) {
            let selectedFieldIds = this.exportTemplate.IncludeFields.split(',').map(Number);

            for (let j = 0; j < selectedFieldIds.length; j++) {
                for (let k = 0; k < available.length; k++) {
                    if (selectedFieldIds[j] == available[k].ID) {
                        this.selectedFields[this.selectedFields.length] = available[k];
                        available[k].ExtOrder = order++;
                    }
                }
            }
        }

        for (let i = 0; i < available.length; i++) {
            if (available[i].ExtOrder == null)
                available[i].ExtOrder = order++;            
        }
        return available;        
    }

    public save() {
        let fields = "";
        var sel = _.sortBy(this.selectedFields, "ExtOrder");
        sel.forEach(function (s) {
            if (fields.length != 0) fields += ",";
            fields += s.ID;
        })
        //trigger save event
        this.saveFieldsClick.emit(fields);
    }

    public top(event, field: FieldDefinition) {
        event.stopPropagation();
        this.isLoading = true;
        console.log(this.availableFields);
        //push everything down        
        field.ExtOrder = 0;
        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID!=field.ID)
                this.availableFields[i].ExtOrder++;
        }
        this.availableFields = _.clone(this.availableFields);
        this.isLoading = false;
    }

    public bottom(event, field: FieldDefinition) {
        event.stopPropagation();
        //push everything up below this item
        this.isLoading = true;
        let found: boolean = false;
        let max: number = 0;
        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID == field.ID)
                found = true;
            if (found) this.availableFields[i].ExtOrder++;
            max = this.availableFields[i].ExtOrder
        }
        field.ExtOrder = max + 1;
        this.availableFields = _.clone(this.availableFields);
        this.isLoading = false;
    }

    public up(event,field: FieldDefinition) {
        event.stopPropagation();
        this.isLoading = true;
        
        for (let i = 0; i < this.availableFields.length; i++) {            
            if (this.availableFields[i].ID == field.ID && i > 0) {
                let order = this.availableFields[i].ExtOrder;
                this.availableFields[i].ExtOrder = this.availableFields[i - 1].ExtOrder;
                this.availableFields[i - 1].ExtOrder = order;
                
            }        
        }      
        this.availableFields = _.clone(this.availableFields);
        this.isLoading = false;
    }

    public down(event,field: FieldDefinition) {
        event.stopPropagation();
        this.isLoading = true;
        
        for (let i = 0; i < this.availableFields.length; i++) {
            if (this.availableFields[i].ID == field.ID && i < this.availableFields.length -1) {
                let order = this.availableFields[i].ExtOrder;
                this.availableFields[i].ExtOrder = this.availableFields[i + 1].ExtOrder;
                this.availableFields[i + 1].ExtOrder = order;
            }
        }       
        this.availableFields = _.clone(this.availableFields);
        this.isLoading = false;
    }
}