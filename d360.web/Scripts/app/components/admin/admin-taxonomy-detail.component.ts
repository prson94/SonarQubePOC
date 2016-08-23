///<reference path="../../es6-shim.d.ts"/>
import { Component, Input} from '@angular/core';
import { Taxonomy } from '../../models/taxonomy.model';
import { MessagesService } from '../../services/index';


@Component({
    selector: 'd3s-admin-model-detail-component',
    template: `
                    <div class="tile tile-detail">                                              
                        <d3s-field-definition-tile [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID" ></d3s-field-definition-tile>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-model-level-tile [(taxonomy)]="taxonomy"></d3s-model-level-tile>
                    </div>                    
                    <div class="tile tile-detail">
                        <d3s-people-responsibilities-tile [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID" [showHidden]="true"></d3s-people-responsibilities-tile>                        
                    </div>                    
                    <div class="tile tile-detail">
                        <d3s-claims-tile [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID" [readonly]="false"></d3s-claims-tile>                 
                    </div>    
                `
})

export class AdminTaxonomyDetailComponent {
    @Input() taxonomy: Taxonomy = null;
    
    constructor() {  }    
}