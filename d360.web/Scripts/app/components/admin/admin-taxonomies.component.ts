///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import {Taxonomy} from '../../models/taxonomy.model';
import { MessagesService, HeaderBreadcrumbService, TaxonomiesService, FieldsService  } from '../../services/index';
import {AdminBaseComponent} from './admin-base.component';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FieldDefinition } from '../../models/fields.model';
import {AdminTaxonomyDetailComponent } from './admin-taxonomy-detail.component';


@Component({
    selector: 'd3s-admin-models-component',    
    directives: [DataTable, Column, TileActionsComponent, AdminTaxonomyDetailComponent],
    providers: [TaxonomiesService, FieldsService],
    template:   `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header>Models
                                <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Model'" (addClick)="addTaxonomy()"></d3s-tile-actions>                            
                            </header>
                            <p-dataTable [value]="taxonomies" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selectedTaxonomy"  >                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column field="TaxonomyTypeClass" header="Classification" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column [style]="{width:'40px'}">
                                    <template let-template="rowData">
                                        <div class="RowTools">
                                            <a style="cursor:pointer;"><i class="fa fa-pencil"></i></a>                                        
                                        </div>
                                    </template>
                                </p-column>                            
                                <p-column  [style]="{width:'40px'}">
                                    <template let-template="rowData">
                                        <div class="RowTools">                                
                                            <a style="cursor:pointer;"><i class="fa fa-trash-o"></i></a>                                    
                                        </div>
                                    </template>
                                </p-column>                            
                            </p-dataTable>
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
    

    constructor(private taxonomiesService: TaxonomiesService, private fieldsService: FieldsService, private messagesService: MessagesService, private headerBreadcrumbService: HeaderBreadcrumbService) {
        super("Models", headerBreadcrumbService);
    }

    ngOnInit() {
        this.getTaxonomies();        
    }

    getTaxonomies() {        
        this.taxonomiesService
            .getTaxonomies()
            .then(taxonomies => this.taxonomies = taxonomies)
            .catch(error => this.error = error); // TODO: Display error message
    }

    
    addTaxonomy() {
        //show new taxonomy ui 
    }

    
}