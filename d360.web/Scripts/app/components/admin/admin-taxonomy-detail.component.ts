///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {DataTable, Column} from 'primeng/primeng';
import { Taxonomy, TaxonomyLevel } from '../../models/taxonomy.model';
import { MessagesService, TaxonomiesService, FieldsService, ResponsibilityService  } from '../../services/index';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { FieldDefinition } from '../../models/fields.model';
import {ResponsibilityItem} from '../../models/responsibility.model';
import { ClaimsTile } from '../tiles/claims.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';


@Component({
    selector: 'd3s-admin-model-detail-component',
    directives: [DataTable, Column, TileActionsComponent, ClaimsTile, FieldDefinitionTile, PeopleResponsibilitiesTile],
    providers: [FieldsService, ResponsibilityService],
    template: `
                    <div class="tile tile-detail">                                              
                        <d3s-field-definition-tile [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID" ></d3s-field-definition-tile>
                    </div>

                    <div class="tile tile-detail">
                        <header>Levels
                            <d3s-tile-actions [hasAdd]="true" [addTitle]="'Add Model Level'" (addClick)="addLevel()"></d3s-tile-actions>                            
                        </header>
                        <p-dataTable [value]="levels" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" >                                                        
                            <p-column field="Level" header="Level" [sortable]="true" [filter]="true"></p-column>                                                            
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="true"></p-column>                                                            
                            <p-column field="Description" header="Description" [sortable]="true" [filter]="true"></p-column>                                                            
                        </p-dataTable>                            
                    </div>                    
                    <div class="tile tile-detail">
                        <d3s-people-responsibilities-tile [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID" [showHidden]="true"></d3s-people-responsibilities-tile>                        
                    </div>                    
                    <div class="tile tile-detail">
                        <d3s-claims-tile [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID" [readonly]="false"></d3s-claims-tile>                 
                    </div>    
                `
})

export class AdminTaxonomyDetailComponent implements OnChanges {
    @Input() taxonomy: Taxonomy = null;
    error: any;
    fields: FieldDefinition[] = [];
    responsibilities: ResponsibilityItem[] = [];
    levels: TaxonomyLevel[] = [];

    constructor(private fieldsService: FieldsService, private responsibilityService: ResponsibilityService, private taxonomiesService: TaxonomiesService) {
        
    }
        

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        console.log('Change detected:', changes['taxonomy'].currentValue);
        if (this.taxonomy != null) this.getDetails();
    }

    getDetails() {
        this.getFields();
        this.getLevels();
        this.getPermissions();
        this.getResponsibilities();
    }
    
    getFields() {         
        this.fieldsService
            .getFields(this.taxonomy.ID, "TaxonomyType")
            .then(fields => this.fields = fields)
            .catch(error => this.error = error); // TODO: Display error message
    }

    getLevels() {
        this.taxonomiesService
            .getTaxonomyLevels(this.taxonomy)
            .then(levels => this.levels = levels)
            .catch(error => this.error = error);
    }

    getPermissions() {

    }

    getResponsibilities() {
        this.responsibilityService
            .getResponsibilityDetail(this.taxonomy.ID, "TaxonomyType")
            .then(responsiblity => this.responsibilities)
            .catch(error => this.error = error);
    }    
}