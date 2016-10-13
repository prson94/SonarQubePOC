import { Input, Output, Component, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { Synonym, SynonymItem, SynonymEditModel } from '../../models/object-detail.model';
import { FormMode, FormHelper } from '../../models/form.model';
import { BaseComponent } from '../shared/base.component';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-synonyms-tile',
    template: `
<div *ngIf="isLoading">
    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
</div>
<div *ngIf="!isLoading">
    <div [ngSwitch]="formMode">
        <div *ngSwitchDefault>
            <header>&nbsp;<d3s-tile-actions *ngIf="!readonly" (addClick)="add();" [hasAdd]="hasAdd"></d3s-tile-actions></header>
            <p-dataTable [value]="items" selectionMode="single" [rows]="20" [paginator]="true" [(selection)]="selectedItem">                
                <p-column field="ObjectTypeName" header="Type" sortable="true"></p-column>
                <p-column header="Parent" sortable="true">
                    <template pTemplate type="body" let-item="rowData">                        
                        <d3s-tooltip [objectType]="item.Object" [objectId]="item.ParentID" [tooltipType]="'Preview'">{{item.ParentName}}</d3s-tooltip>
                    </template>
                </p-column>
                <p-column header="Name" sortable="true">
                    <template pTemplate type="body" let-item="rowData">                        
                        <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" [tooltipType]="'Preview'">{{item.Name}}</d3s-tooltip>
                    </template>
                </p-column>
                <p-column field="SubjectArea" [header]="subjectAreaName" sortable="true"></p-column>
                <p-column [style]="{ 'width': '28px' }">
                    <template let-col let-item="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" [tooltipType]="'Preview'" [icon]="'info'"></d3s-tooltip>
                        </div>
                    </template> 
                </p-column>
                <p-column *ngIf="!readonly && hasDelete" [style]="{ 'width': '28px' }">
                    <template let-col let-item="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a (click)="selectedItem=item;delete();" style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>
                        </div>
                    </template> 
                </p-column>
            </p-dataTable>
        </div>
        <div *ngSwitchCase="FormMode.Adding">
            <h4>Add Synonym</h4>
            <div class="row">
                <div class="col s12">
                <div class="FieldName">Synonym</div>
                <select [(ngModel)]="selectedSynonym" style="width:300px;display:block;">
                    <option *ngFor="let i of synonymItems" [value]="i.ID">{{i.Name}}</option>
                </select>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Save" (click)="save();"></button><button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default;"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.Deleting">
            <delete-form [uri]="'/form/DeleteSynonymByID?id=' + selectedItem.ObjectID + '&type=' + selectedItem.Object + '&intersectMapID=' + selectedItem.IntersectMapID"
                         [method]="'delete'"
                         [prompt]="'Are you sure you want to remove ' + selectedItem.Name + '?'"
                         (onDeleteSuccess)="load();formMode = FormMode.Default;"
                         (onCancel)="formMode = FormMode.Default;">
            </delete-form>
        </div>
    </div>
</div>
`,
    providers: [ObjectDetailService],
})

export class SynonymsTile extends BaseComponent implements OnChanges, OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean = true;
    @Output() itemCount: number = 0; 
    
    @Input() hasAdd: boolean = true;    
    @Input() hasDelete: boolean = true;

    
    private formMode = FormMode.Default;
    private FormMode = FormMode;
    private items;
    private selectedItem;

    private synonymItems;
    private typeIsSubject;
    private selectedSynonym;
    private subjectAreaName = 'SubjectArea';
    private areSynonymOptionsLoaded: boolean = false;

    constructor(private objectDetailService: ObjectDetailService) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.ArtifactType_TaxonomyTypeID && CompanySettings.ArtifactType_TaxonomyTypeID != '') {
            this.subjectAreaName = CompanySettings.ArtifactType_TaxonomyTypeID;
        }
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;
        this.objectDetailService.getObjectSynonyms(this.objectID, this.objectType)
            .then(d => {
                this.items = d;
                this.itemCount = this.items.length;
                this.isLoading = false;
            });            
    }

    add() {
        // only load synonym optons when we need to add things.
        if (!this.areSynonymOptionsLoaded) {
            this.isLoading = true;
            this.objectDetailService.getSynonymOptions(this.objectID, this.objectType)
                .then(d => {
                    this.typeIsSubject = d.typeIsSubject;
                    this.synonymItems = d.items;
                    this.formMode = FormMode.Adding;
                    this.areSynonymOptionsLoaded = true;
                    this.isLoading = false;
                });
        }
        else
            this.formMode = FormMode.Adding;
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {

        this.isLoading = true;
        var model = new SynonymEditModel();
        model.Synonym = this.selectedSynonym;
        model.ID = this.objectID;
        model.Type = this.objectType;
        model.TypeIsSubject = this.typeIsSubject


        this.objectDetailService.postSynonym(model)
            .then(d => {
                this.formMode = FormMode.Default;
                this.load();
            });
    }
}
