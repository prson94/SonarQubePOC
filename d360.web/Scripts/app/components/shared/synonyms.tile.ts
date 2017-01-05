import { Input, Output, Component, OnChanges, SimpleChange, OnInit } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { Synonym, SynonymItem, SynonymEditModel } from '../../models/object-detail.model';
import { FormMode, FormHelper } from '../../models/form.model';
import { BaseComponent } from '../shared/base.component';
import { MessagesService } from '../../services/messages.service';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Predicate } from '../../models/predicate.model';
import { PredicatesService } from '../../services/predicates.service';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-synonyms-tile',
    styles: [
        `
        p-autoComplete>span>input {
         width:100%;
        }
    `],
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <div [ngSwitch]="formMode">
            <div *ngSwitchDefault>
                <header>&nbsp;<d3s-tile-actions *ngIf="!readonly" (addClick)="add();" [hasAdd]="hasAdd"></d3s-tile-actions></header>
                <input #gb type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="items" selectionMode="single" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" paginator="true" [(selection)]="selectedItem">                
                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                    <p-column header="Name" field="Name" sortable="true">
                        <template pTemplate type="body" let-item="rowData">                        
                            <d3s-tooltip *ngIf="item.Object" [objectType]="item.Object" [objectId]="item.ObjectID" [tooltipType]="'Preview'">
                                <a (click)="navigate(item.Url)">{{item.Name}}</a>
                            </d3s-tooltip>
                            <span *ngIf="!item.Object">{{item.Name}}</span>
                        </template>
                    </p-column>
                    <p-column field="ObjectTypeName" header="Type" sortable="true"></p-column>
                    <p-column field="Predicate" header="Predicate" sortable="true"></p-column>
                    <p-column header="Parent" field="ParentName" sortable="true">
                        <template pTemplate type="body" let-item="rowData">                        
                            <d3s-tooltip [objectType]="item.Object" [objectId]="item.ParentID" [tooltipType]="'Preview'">
                                <a (click)="navigate(item.ParentUrl)">{{item.ParentName}}</a>
                            </d3s-tooltip>
                        </template>
                    </p-column>                
                    <p-column field="SubjectArea" [header]="subjectAreaName" sortable="true">
                        <template pTemplate type="body" let-item="rowData">                        
                            <d3s-tooltip *ngIf="item.TaxonomyTypeID != null" [objectType]="'TaxonomyType'" [objectId]="item.TaxonomyTypeID" [tooltipType]="'Preview'">
                                <a (click)="navigateTaxonomy(item)">{{item.SubjectArea}}</a>
                            </d3s-tooltip>
                        </template>
                    </p-column>
                    <p-column *ngIf="!readonly && hasDelete" [style]="{ 'width': '48px' }">
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
                <div class="row">
                    <div class="col s12">
                        <div class="FieldName" style="display:block;">Synonym Type</div>
                        <select [(ngModel)]="selectedType" style="width:35em;" (ngModelChanged)="clearSearch()">
                            <option></option>
                            <option *ngFor="let i of synonymTypes" [value]="i.Value">
                                {{i.Name}}
                            </option>
                            <option value="_custom">Custom</option>
                        </select>
                    </div>
                </div>
                <div class="row" style="padding-bottom: 15px" *ngIf="selectedType != '_custom'">
                    <div class="col s12">
                        <div class="FieldName" style="display:block;">Synonym</div>
                        <p-autoComplete [suggestions]="synonymItems" (completeMethod)="search($event)" field="Name" [(ngModel)]="selectedSynonym" placeholder="Search..." size="64" [disabled]="selectedType == ''"></p-autoComplete>
                        <span *ngIf="isLoadingItems"><i class="fa fa-spinner fa-spin"></i></span>
                    </div>
                </div>
                <div class="row" style="padding-bottom: 15px" *ngIf="selectedType == '_custom'">
                    <div class="col s12">
                        <div class="FieldName">Synonym</div>         
                        <div><input maxlength="250" pInputText name="name" type="text" style="width:35em;" [(ngModel)]="customSynonymName" required /></div>
                    </div>
                    <div class="col s12">
                        <div class="FieldName">Synonym Type</div>         
                        <div>
                            <select [(ngModel)]="predicateType" style="width:35em;">
                            <option></option>
                            <option *ngFor="let i of predicateTypes" [value]="i.ID">
                                {{i.Name}}
                            </option>                            
                            </select>
                        </div>
                    </div>
                </div>
                <div class="row">
                    <div class="col s12">
                        <button pButton type="button" label="Save" (click)="save();" [disabled]="(selectedType != '_custom' && selectedSynonym?.ID == null) || (selectedType == '_custom' && (!predicateType || !customSynonymName))"></button><button pButton type="button" label="Cancel" (click)="formMode = FormMode.Default;"></button>
                    </div>
                </div>
            </div>             
            <d3s-delete-form *ngSwitchCase="FormMode.Deleting"
                    [callback]="theDeleteCallback"
                    [itemId]="selectedItem"
                    [method]="'callback'"
                    [prompt]="'Are you sure you want to remove the synonym ' + selectedItem.Name + '?'"                                         
                    (onCancel)="formMode = FormMode.Default;"
            ></d3s-delete-form>              
        </div>
    </div>
    `,
    providers: [ObjectDetailService, PredicatesService],
})

export class SynonymsTile extends BaseComponent implements OnChanges, OnInit {
    @Input() objectType: string;
    @Input() objectID: number;
    @Input() readonly: boolean = true;
    @Output() itemCount: number = 0; 
    
    @Input() hasAdd: boolean = true;    
    @Input() hasDelete: boolean = true;

    theDeleteCallback: Function;

    private defaultSort = [
        { field: 'ObjectTypeName', order: -1 },
        { field: 'ParentName', order: -1 },
        { field: 'Name', order: -1 }
    ];
    
    private formMode = FormMode.Default;
    private FormMode = FormMode;
    private items: Synonym[] = [];
    private selectedItem;


    private synonymTypes = [];
    private predicateTypes: Predicate[] = [];
    private predicateType: number = 0;
    private selectedType: string = '';
    private synonymItems = [];
    private selectedSynonym: SynonymItem;
    private subjectAreaName : string = 'SubjectArea';
    private areSynonymOptionsLoaded: boolean = false;
    private customSynonymName: string = '';

    private isLoadingItems = false;

    constructor(private messagesService: MessagesService, private objectDetailService: ObjectDetailService, private router: Router, private predicatesService: PredicatesService) {
        super();

        this.theDeleteCallback = this.deleteSynonym.bind(this);
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

    private deleteSynonym(item: Synonym) {        
        this.isLoading = true;
        this.objectDetailService.deleteSynonym(item).then(
            res => {
                this.isLoading = false;
                this.showMessageForResult(this.messagesService, res);
                if (item.IntersectID > 0)
                    this.items = this.items.filter(x => x.IntersectID != item.IntersectID);
                else if (item.CustomID > 0)
                    this.items = this.items.filter(x => x.CustomID != item.CustomID);
                this.itemCount = this.items.length;
                this.formMode = FormMode.Default;
            });
    }

    add() {
        this.selectedSynonym = null;
        //if we havent loaded predicate types yet do so
        if (this.predicateTypes.length == 0) {
            this.predicatesService.getPredicates().then(
                res => {
                    this.predicateTypes = res.filter(x => x.Type == 'Synonym'); //synonym types only
                });
        }

        //if we havent loaded synonym types already do so now
        if (this.synonymTypes.length == 0) {
            this.objectDetailService.getSynonymTypes(this.objectID, this.objectType)
                .then(d => {
                    this.synonymTypes = d;
                    this.formMode = FormMode.Adding;
                });
        }
        else {
            this.formMode = FormMode.Adding;
        }
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        this.isLoading = true;

        if (this.selectedSynonym && this.selectedSynonym.ID) {
            var model = new SynonymEditModel();
            model.Synonym = this.selectedSynonym.ID;
            model.ID = this.objectID;
            model.Type = this.objectType;
            model.TypeIsSubject = this.selectedSynonym.TargetingSubject;

            this.objectDetailService.postSynonym(model)
                .then(d => {
                    this.showMessageForResult(this.messagesService, d);
                    this.formMode = FormMode.Default;
                    this.load();
                });
        }
        else if (this.customSynonymName) {            
            this.objectDetailService.postCustomSynonym(this.customSynonymName, this.predicateType, this.objectType, this.objectID)
                .then(d => {
                    this.showMessageForResult(this.messagesService, d);
                    this.customSynonymName = '';
                    this.formMode = FormMode.Default;
                    this.load();
                });
        }
    }
    
    navigate(url: string) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
    }

    navigateTaxonomy(item: Synonym) {
        this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('TaxonomyType', item.TaxonomyTypeID));
    }

    search(e: any) {
        this.isLoadingItems = true;
        let type = this.synonymTypes.find(t => t.Value == this.selectedType);
        if (!type) {
            this.isLoadingItems = false;
            return;
        }
        this.objectDetailService.getSynonymOptions(type.Object, type.ObjectID, this.objectType, this.objectID, (e.query || ''))
            .then(r => {
                this.isLoadingItems = false;
                this.synonymItems = r.items;
            })
            .catch(() => this.isLoadingItems = false);
    }

    clearSearch() {
        this.synonymItems = [];
        this.selectedSynonym = null;
    }
}
