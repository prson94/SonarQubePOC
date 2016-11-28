import { Component, OnInit, OnDestroy} from '@angular/core';
import { Taxonomy} from '../../models/taxonomy.model';
import { MessagesService, HeaderBreadcrumbService, TaxonomiesService, FieldsService, RightSidebarService, StateService } from '../../services/index';
import { AdminBaseComponent} from './admin-base.component';
import { FieldDefinition } from '../../models/fields.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../models/rightsidebar.model';

@Component({
    selector: 'd3s-admin-models-component',    
    providers: [TaxonomiesService, FieldsService],
    template:   `<d3s-audit *ngIf="isAuditVisible" [objectID]="selectedTaxonomy?.ID" [objectName]="selectedTaxonomy?.Name" [objectType]="'TaxonomyType'"></d3s-audit>
                <d3s-admin-model-classifications *ngIf="isClassificationsVisible" ></d3s-admin-model-classifications>
                <div *ngIf="showEditor || showDelete && !isAuditVisible && !isLoading && !isClassificationsVisible" class="row">
                    <div class="tile tile-detail">                            
                            <d3s-admin-model-editor *ngIf="showEditor" [taxonomy]="selectedTaxonomy" (saveClick)="saveModel($event)" (closeClick)="closeEditor()"></d3s-admin-model-editor>
                            <d3s-delete-form *ngIf="showDelete"
                                        [callback]="theDeleteCallback"
                                        [itemId]="selectedTaxonomy?.ID"
                                         [method]="'callback'"
                                         [prompt]="'Are you sure you want to delete the model [' + [selectedTaxonomy?.Name] + ']?'"                                         
                                         (onCancel)="showDelete=false;"
                            ></d3s-delete-form>
                    </div>
                </div>
                <div *ngIf="!showEditor && !showDelete && !isAuditVisible && !isClassificationsVisible" class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Models
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading">
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="taxonomies" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selectedTaxonomy"  (onRowDblclick)="selectedTaxonomy=$event.data;showEditor=true;" >                                                        
                                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                            
                                    <p-column field="TaxonomyTypeClass" header="Classification" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                            
                                    <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                            
                                    <p-column [style]="{width:'40px'}">
                                        <template let-model="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selectedTaxonomy=model;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <template let-model="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selectedTaxonomy=model;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </template>
                                    </p-column>                            
                                </p-dataTable>
                            </span>                            
                        </div>
                    </div>
                    <div class="col l8 s12" *ngIf="selectedTaxonomy">                                            
                        <d3s-admin-model-detail-component [(taxonomy)]="selectedTaxonomy"></d3s-admin-model-detail-component>
                    </div>
                </div>  
                `
})

export class AdminTaxonomiesComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    taxonomies: Taxonomy[] = [];    
    error: any;
    selectedTaxonomy: Taxonomy = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    isClassificationsVisible: boolean = false;

    constructor(private stateService: StateService, rightSidebarService: RightSidebarService,  private taxonomiesService: TaxonomiesService, private fieldsService: FieldsService, private messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Models";
        this.setCommonItems();
        this.setCommonRightSideBar(true);

        this.rightSidebarService.showItem(new RightSidebarItem('Classification', 'classifications', ['fa-tag']));
    }

    ngOnInit() {
        this.getTaxonomies();        
        this.theDeleteCallback = this.deleteTaxonomy.bind(this);        
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    getTaxonomies() {
        this.isLoading = true;     
        this.taxonomiesService
            .getTaxonomies()
            .then(taxonomies => {
                this.taxonomies = taxonomies;
                if (this.taxonomies.length && this.taxonomies.length > 0) {
                    this.selectedTaxonomy = this.taxonomies[0];
                }
                this.isLoading = false;
            })
            .catch(error => this.error = error); // TODO: Display error message
    }

    
    add() {
        this.selectedTaxonomy = null;
        this.showEditor = true;                
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selectedTaxonomy == null && this.taxonomies.length > 0) this.selectedTaxonomy = this.taxonomies[0];
    }

    saveModel(event) {        
        this.taxonomiesService
             .saveTaxonomy(event.taxonomy)
            .then(response => {                
                this.showEditor = false;
                if (response.type == 'error') {                    
                    this.selectedTaxonomy = this.taxonomies.length > 0 ? this.taxonomies[0] : null;
                }
                else {                                        
                    
                    if (event.action == "new") {
                        event.taxonomy.ID = Number(response.id);
                        event.taxonomy.Class = undefined;                        
                        this.taxonomies[this.taxonomies.length] = event.taxonomy;
                    }
                    else {
                        var index = this.taxonomies.findIndex(x => x.ID == event.taxonomy.ID);                        
                        if (index >= 0)
                            this.taxonomies[index] = event.taxonomy;
                    }                    
                    this.selectedTaxonomy = event.taxonomy;
                }
                this.showMessageForResult(this.messagesService, response);
                this.stateService.reloadLeftNavMenu();
            })
             .catch(error => this.error = error);        
    }

    deleteTaxonomy(id : number) {
        this.taxonomiesService.deleteTaxonomy(id)
            .then(res => {                
                this.showMessageForResult(this.messagesService, res);

                if (res.type != 'error') {                    
                    this.taxonomies = this.taxonomies.filter(x => x.ID != id);                    
                    this.selectedTaxonomy = this.taxonomies.length > 0 ? this.taxonomies[0] : null;
                    this.stateService.reloadLeftNavMenu();
                }
                this.showDelete = false;
            });
    }
        
    protected showHideBreadcrumbItem(activatedItem: RightSidebarItem) {
        if (activatedItem.tag == 'classifications') this.isClassificationsVisible = !this.isClassificationsVisible;
    }
}