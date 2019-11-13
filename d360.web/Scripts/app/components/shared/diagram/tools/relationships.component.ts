import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonComponentAssetResult } from '../../../../models/asset-search.model';


declare var CompanySettings;

@Component({
    selector: 'd3s-diagram-relationships',
    templateUrl: 'relationships.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DiagramAssetRelationshipComponent {
    private assetSearchSelection: CommonComponentAssetResult[] = [];

    constructor() { }

    onAssetSearchSelection(event: any) {
        console.warn("Search selection event triggered!");
        console.warn("Event:",event);
    }

}


