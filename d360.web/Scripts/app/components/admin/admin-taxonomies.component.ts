///<reference path="../../es6-shim.d.ts"/>
import { Component} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import {Taxonomy} from '../../models/taxonomy.model';
import { MessagesService, HeaderBreadcrumbService, TaxonomiesService  } from '../../services/index';


@Component({
    selector: 'd3s-admin-models-component',    
    directives: [DataTable, Column],
    providers: [TaxonomiesService],
    template:   `<div class="row">
                    <div class="col l4 s12">                    
                        <div class="tile tile-detail">
                            <header>Models
                                <d3s-tile-actions [hasAdd]="true" (addClick)="addTaxonomy()"></d3s-tile-actions>                            
                            </header>
                            <p-dataTable [value]="taxonomies" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" >                                                        
                                <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column field="TaxonomyTypeClass" header="Classification" [sortable]="true" [filter]="true"></p-column>                            
                                <p-column field="MaximumDepth" header="Max Depth" [sortable]="true" [filter]="true"></p-column>                            
                            </p-dataTable>
                        </div>
                    </div>
                </div>  
                `
})

export class AdminTaxonomiesComponent {
    taxonomies: Taxonomy[] = [];
    error: any;

    constructor(private taxonomiesService: TaxonomiesService, private messagesService: MessagesService) { }

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