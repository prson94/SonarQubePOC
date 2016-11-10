import { Input, Output, Component, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { Synonym, SynonymItem, SynonymEditModel } from '../../models/object-detail.model';
import { FormMode, FormHelper } from '../../models/form.model';
import { BaseComponent } from '../shared/base.component';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-synonyms-tile',
    styles: [
    `
    p-autoComplete>span>input {
     width:100%;
    }
`]
    ,
    template: `
<div *ngIf="isLoading">
    <div style="width:100%;text-align:center;"><i class="fa fa-spinner fa-spin"></i></div>
</div>
<div *ngIf="!isLoading">
    <div [ngSwitch]="formMode">
        <div *ngSwitchDefault>
            <header>&nbsp;<d3s-tile-actions *ngIf="!readonly" (addClick)="add();" [hasAdd]="hasAdd"></d3s-tile-actions></header>
            <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
            <p-dataTable #dt [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" [(selection)]="selectedItem" sortField="ObjectTypeName" sortOrder="-1">                
                <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                <p-column field="ObjectTypeName" header="Type" sortable="custom" (sortFunction)="caseInsensitiveSort($event)"></p-column>
                <p-column header="Parent" field="ParentName" sortable="custom" (sortFunction)="caseInsensitiveSort($event)">
                    <template pTemplate type="body" let-item="rowData">                        
                        <d3s-tooltip [objectType]="item.Object" [objectId]="item.ParentID" [tooltipType]="'Preview'">
                            <a (click)="navigate(item.ParentUrl)">{{item.ParentName}}</a>
                        </d3s-tooltip>
                    </template>
                </p-column>
                <p-column header="Name" field="Name" sortable="custom" (sortFunction)="caseInsensitiveSort($event)">
                    <template pTemplate type="body" let-item="rowData">                        
                        <d3s-tooltip [objectType]="item.Object" [objectId]="item.ObjectID" [tooltipType]="'Preview'">
                            <a (click)="navigate(item.Url)">{{item.Name}}</a>
                        </d3s-tooltip>
                    </template>
                </p-column>
                <p-column field="SubjectArea" [header]="subjectAreaName" sortable="custom" (sortFunction)="caseInsensitiveSort($event)">
                    <template pTemplate type="body" let-item="rowData">                        
                        <d3s-tooltip *ngIf="item.TaxonomyTypeID != null" [objectType]="'TaxonomyType'" [objectId]="item.TaxonomyTypeID" [tooltipType]="'Preview'">
                            <a (click)="navigateTaxonomy(item)">{{item.SubjectArea}}</a>
                        </d3s-tooltip>
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
            <header>Add Synonym</header>
            <div class="row" style="padding-bottom: 15px">
                <div class="col s12">
                <div class="FieldName" style="display:block;">Synonym</div>
                <p-autoComplete [suggestions]="synonymItems" (completeMethod)="search($event)" field="Name" [(ngModel)]="selectedSynonym" placeholder="Search..." size="65"></p-autoComplete>
                <span *ngIf="isLoadingItems"><i class="fa fa-spinner fa-spin"></i></span>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <button pButton type="button" label="Save" (click)="save();"></button><button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default;"></button>
                </div>
            </div>
        </div>
        <div *ngSwitchCase="FormMode.Deleting">
            <delete-form [uri]="'/form/DeleteSynonymByID?id=' + selectedItem.IntersectID"
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

    private defaultSort = [
        { field: 'ObjectTypeName', order: -1 },
        { field: 'ParentName', order: -1 },
        { field: 'Name', order: -1 }
    ];
    
    private formMode = FormMode.Default;
    private FormMode = FormMode;
    private items;
    private selectedItem;

    private synonymItems = [];
    private selectedSynonym: SynonymItem;
    private subjectAreaName = 'SubjectArea';
    private areSynonymOptionsLoaded: boolean = false;

    private isLoadingItems = false;

    constructor(private objectDetailService: ObjectDetailService, private router: Router) {
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
                //console.log(d);
                this.itemCount = this.items.length;
                this.isLoading = false;
            });            
    }

    add() {
        this.selectedSynonym = null;
        this.formMode = FormMode.Adding;
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {

        this.isLoading = true;
        var model = new SynonymEditModel();
        model.Synonym = this.selectedSynonym.ID;
        model.ID = this.objectID;
        model.Type = this.objectType;
        model.TypeIsSubject = this.selectedSynonym.TargetingSubject;


        this.objectDetailService.postSynonym(model)
            .then(d => {
                this.formMode = FormMode.Default;
                this.load();
            });
    }

    caseInsensitiveSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending        
        this.items = _.orderBy(this.items, [item => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order == -1 ? 'desc' : 'asc']);        
    }

    navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }

    navigateTaxonomy(item: Synonym) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TaxonomyType', item.TaxonomyTypeID));
    }

    search(e: any) {
        this.isLoadingItems = true;
        if (e.query == null || e.query == '') {
            this.isLoadingItems = false;
            return;
        }
        this.objectDetailService.getSynonymOptions(this.objectID, this.objectType, e.query)
            .then(r => {
                this.isLoadingItems = false;
                this.synonymItems = r.items;
            })
            .catch(() => this.isLoadingItems = false);
    }
}
