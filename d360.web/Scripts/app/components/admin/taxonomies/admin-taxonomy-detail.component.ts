import { Component, Input} from '@angular/core';
import { Taxonomy } from '../../../models/taxonomy.model';

@Component({
    selector: 'd3s-admin-model-detail-component',
    template: `
                    <div class="tile tile-detail">
                        <d3s-field-definition-tile objectType="TaxonomyType" [objectID]="taxonomy?.ID" ></d3s-field-definition-tile>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-admin-level-grid objectType="TaxonomyType" [maxDepth]="taxonomy?.MaximumDepth" [objectId]="taxonomy?.ID"></d3s-admin-level-grid>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-people-responsibilities-tile objectType="TaxonomyType" [objectID]="taxonomy?.ID" showHidden="true"></d3s-people-responsibilities-tile>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-claims-tile objectType="TaxonomyType" [objectID]="taxonomy?.ID" readonly="false"></d3s-claims-tile>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-admin-allocation [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID"></d3s-admin-allocation>
                    </div>                                
                `
})

export class AdminTaxonomyDetailComponent {
    @Input() taxonomy: Taxonomy = null;    
}