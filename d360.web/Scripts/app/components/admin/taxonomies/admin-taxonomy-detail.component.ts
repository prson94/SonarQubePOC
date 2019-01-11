import { Component, Input} from '@angular/core';
import { Taxonomy } from '../../../models/taxonomy.model';

@Component({
    selector: 'd3s-admin-model-detail-component',
    template: `
                    <div class="tile tile-detail">
                        <object-detail [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID"></object-detail>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-field-definition-tile objectType="TaxonomyType" [objectID]="taxonomy?.ID" ></d3s-field-definition-tile>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-admin-level-grid objectType="TaxonomyType" [maxDepth]="taxonomy?.MaximumDepth" [objectId]="taxonomy?.ID"></d3s-admin-level-grid>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-responsibility-relations queryType="A" [id]="taxonomy?.AssetTypeID" [showAddButton]="false"></d3s-responsibility-relations>
                    </div>
                    <div class="tile tile-detail">
                        <d3s-admin-allocation [objectType]="'TaxonomyType'" [objectID]="taxonomy?.ID"></d3s-admin-allocation>
                    </div>                                
                `
})

export class AdminTaxonomyDetailComponent {
    @Input() taxonomy: Taxonomy = null;    
}