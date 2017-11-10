import { Component, OnInit, OnDestroy} from '@angular/core';
import { Taxonomy} from '../../../models/taxonomy.model';
import { MessagesService } from '../../../services/messages.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { TaxonomiesService } from '../../../services/taxonomies.service';
import { FieldsService } from '../../../services/fields.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { StateService } from '../../../services/state.service';
import { AdminBaseComponent} from '../admin-base.component';
import { FieldDefinition } from '../../../models/fields.model';
import { Title } from '@angular/platform-browser';
import { RightSidebarItem } from '../../../models/rightsidebar.model';
import { AssetTypeService } from "../../../services/asset-type.services";

@Component({
    selector: 'd3s-admin-models-component',    
    providers: [TaxonomiesService, FieldsService, AssetTypeService],
    template:   `<div *ngIf="showEditor || showDelete && !isLoading" class="row">
                    <div class="tile tile-detail">     
                        <d3s-asset-type-editor-form *ngIf="showEditor" [assetTypeClass]="'M'" [id]="selectedTaxonomy?.AssetTypeID" [title]="'Edit Model Type'" (onCancel)="closeEditor()" (onComplete)="saveModel($event)"></d3s-asset-type-editor-form>
                        <d3s-delete-form *ngIf="showDelete"
                                    [callback]="theDeleteCallback"
                                    [itemId]="selectedTaxonomy?.AssetTypeID"
                                        [method]="'callback'"
                                        [prompt]="'Are you sure you want to delete the model [' + [selectedTaxonomy?.Name] + ']?'"                                         
                                        (onCancel)="showDelete=false;"
                        ></d3s-delete-form>
                    </div>
                </div>
                <div *ngIf="!showEditor && !showDelete" class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Models
                                <d3s-tile-actions [hasAdd]="true" (addClick)="add()" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions>                            
                            </header>
                            <d3s-loading [isLoading]="isLoading"></d3s-loading>
                            <span *ngIf="!isLoading">
                                <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">
                                <p-dataTable #dt sortField="Name" [sortOrder]="1" [globalFilter]="gb" [value]="taxonomies" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selectedTaxonomy"  (onRowDblclick)="selectedTaxonomy=$event.data;showEditor=true;" >                                                        
                                    <p-footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></p-footer>
                                    <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter"></p-column>                            
                                    <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [filter]="!showSimpleFilter" [style]="{width:'100px'}"></p-column>                            
                                    <p-column [style]="{width:'40px'}">
                                        <ng-template let-model="rowData" pTemplate type="body">
                                            <div class="RowTools">
                                                <a style="cursor:pointer;" (click)="selectedTaxonomy=model;showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                            </div>
                                        </ng-template>
                                    </p-column>                            
                                    <p-column  [style]="{width:'40px'}">
                                        <ng-template let-model="rowData" pTemplate type="body">
                                            <div class="RowTools">                                
                                                <a style="cursor:pointer;" (click)="selectedTaxonomy=model;showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                            </div>
                                        </ng-template>
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

    protected assetTypeService: AssetTypeService = null;

    constructor(private stateService: StateService,
        assetTypeService: AssetTypeService,
        rightSidebarService: RightSidebarService,
        private taxonomiesService: TaxonomiesService,
        private fieldsService: FieldsService,
        private messagesService: MessagesService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title) {

        super(headerBreadcrumbService, titleService, rightSidebarService);    
        this.assetTypeService = assetTypeService;

        this.areaName = "Models";
        this.setCommonItems();
        this.setCommonRightSideBar(true);
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/TaxonomyType/${this.selectedTaxonomy.ID}`
            });
        }
        this.rightSidebarService.showItem(new RightSidebarItem('Classification', 'classifications', ['fa-tag'], 'admin/classification/TaxonomyTypeClass'));
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
        this.showEditor = false;
        //if (response.type == 'error') {
        //    this.selectedTaxonomy = this.taxonomies.length > 0 ? this.taxonomies[0] : null;
        //}
        //else {

        //    if (event.action == "new") {
        //        event.taxonomy.ID = Number(response.id);
        //        event.taxonomy.Class = undefined;
        //        this.taxonomies[this.taxonomies.length] = event.taxonomy;
        //    }
        //    else {
        //        var index = this.taxonomies.findIndex(x => x.ID == event.taxonomy.ID);
        //        if (index >= 0)
        //            this.taxonomies[index] = event.taxonomy;
        //    }
        //    this.selectedTaxonomy = event.taxonomy;
        //}
        //this.showMessageForResult(this.messagesService, response);
        this.getTaxonomies();
        this.stateService.reloadLeftNavMenu();
    }

    deleteTaxonomy(id : number) {
        this.assetTypeService.deleteAssetType(id)
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
}