///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import {Taxonomy} from '../../models/taxonomy.model';
import { MessagesService, HeaderBreadcrumbService, TaxonomiesService, FieldsService, PageHeader  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FieldDefinition } from '../../models/fields.model';
import {AdminTaxonomyDetailComponent } from './admin-taxonomy-detail.component';
import {AdminTaxonomyEditorComponent } from './admin-taxonomy-editor.component';
import {DeleteForm} from '../forms/delete.form';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-admin-models-component',    
    directives: [DataTable, Column, TileActionsComponent, AdminTaxonomyDetailComponent, AdminTaxonomyEditorComponent, DeleteForm],
    providers: [TaxonomiesService, FieldsService],
    template:   `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header *ngIf="!showEditor">Models
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Model'" (addClick)="add()"></d3s-tile-actions>                            
                            </header>
                            <div *ngIf="isLoading">
                                <div style="padding:10px;text-align:center;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                            </div>                         
                            <p-dataTable *ngIf="!showEditor && !showDelete && !isLoading" [value]="taxonomies" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selectedTaxonomy"  (onRowDblclick)="showEditor=true;" >                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column field="TaxonomyTypeClass" header="Classification" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column [style]="{width:'40px'}">
                                    <template let-template="rowData">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <template let-template="rowData">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;" (click)="showDelete=true"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                                </p-column>                            
                            </p-dataTable>
                            <d3s-admin-model-editor *ngIf="showEditor" [taxonomy]="selectedTaxonomy" (saveClick)="saveModel($event)" (closeClick)="closeEditor()"></d3s-admin-model-editor>
                            <delete-form *ngIf="showDelete"
                                        [callback]="theDeleteCallback"
                                        [itemId]="selectedTaxonomy?.ID"
                                         [method]="'callback'"
                                         [prompt]="'Are you sure you want to delete the model [' + [selectedTaxonomy?.Name] + ']?'"                                         
                                         (onCancel)="showDelete=false;"
                                ></delete-form>
                        </div>
                    </div>
                    <div class="col l8 s12">                                            
                        <d3s-admin-model-detail-component [(taxonomy)]="selectedTaxonomy"></d3s-admin-model-detail-component>
                    </div>
                </div>  
                `
})

export class AdminTaxonomiesComponent extends AdminBaseComponent {
    taxonomies: Taxonomy[] = [];    
    error: any;
    selectedTaxonomy: Taxonomy = null;
    showEditor: boolean = false;
    showDelete: boolean = false;
    theDeleteCallback: Function;
    

    constructor(pageHeader: PageHeader, private taxonomiesService: TaxonomiesService, private fieldsService: FieldsService, private messagesService: MessagesService, headerBreadcrumbService: HeaderBreadcrumbService, titleService: Title) {
        super(headerBreadcrumbService, pageHeader, titleService);
        this.areaDescription = "All top-level information models for the organization are defined here. To add a new top-level model, go under Actions and select Add Type.";
        this.areaName = "Models";
        this.setCommonItems();
    }

    ngOnInit() {
        this.getTaxonomies();        
        this.theDeleteCallback = this.deleteTaxonomy.bind(this);        
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
                let actionName = "Created";
                if (event.action == "new") {
                    event.taxonomy.ID = Number(response.id);
                    event.taxonomy.Class = undefined;
                    this.taxonomies[this.taxonomies.length] = event.taxonomy;                     
                }
                else {
                    var index = this.findTaxonomyIndex(event.taxonomy.ID);
                    actionName = "Edited";
                    if (index >= 0)
                        this.taxonomies[index] = event.taxonomy;
                }
                this.messagesService.showInfoMessage("Success", `${actionName} model [${event.taxonomy.Name}] Successfully`);
                this.selectedTaxonomy = event.taxonomy;                
            })
             .catch(error => this.error = error);        
    }

    deleteTaxonomy(id : number) {
        this.taxonomiesService.deleteTaxonomy(id);
        let index = this.findTaxonomyIndex(id);
        let name = this.taxonomies[index].Name;

        this.taxonomies.splice(index, 1);    
        this.messagesService.showInfoMessage("Success", `Deleted model [${name}]`);   
        this.showDelete = false;
        this.selectedTaxonomy = this.taxonomies.length > 0 ? this.taxonomies[0] : null;
    }

    findTaxonomyIndex(id: number) {
        var index: number = -1;
        for (var taxonomy of this.taxonomies) {
            index++;
            if (taxonomy.ID == id) return index;
        }
    }
}