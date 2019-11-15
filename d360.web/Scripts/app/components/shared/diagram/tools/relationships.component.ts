import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonComponentAssetResult, CommonComponentAssetTypeFilter } from '../../../../models/asset-search.model';
import { PredicateType } from '../../../../models/predicate.model';
import { AssetTypeClass } from '../../../../models/asset.model';


declare var CompanySettings;

@Component({
    selector: 'd3s-diagram-relationships',
    templateUrl: 'relationships.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class DiagramAssetRelationshipComponent implements OnInit {
    private sourceAssets: CommonComponentAssetResult[] = [];
    private sourcePrePop: CommonComponentAssetResult[] = [];
    private sourceAssetFilters: CommonComponentAssetTypeFilter[] = [];

    private transformationAsset: CommonComponentAssetResult[] = [];
    private targetAssets: CommonComponentAssetResult[] = [];

    private predicateType: PredicateType = PredicateType.Simple;
    constructor() { }

    ngOnInit() {
        
    }

    addSourceAssetFilter() {
        var filter = new CommonComponentAssetTypeFilter();
        filter.Class = AssetTypeClass.Policy;
        filter.Uid = '8f492762-e3ae-421d-a9ec-5e2cd81331cb';
        this.sourceAssetFilters.push(filter);
    }

    prepopulateSourceAssets() {
        var json = `[{"AssetTypeUid":"1d8734e0-bfcb-4998-99e7-d49abeef83e1","Uid":"d71db234-95b2-4b0b-8575-d8fdc9d6b0f9","Predicate":null,"Segments":[{"Value":"07 testing item"}]},{"AssetTypeUid":"1d8734e0-bfcb-4998-99e7-d49abeef83e1","Uid":"d3dbf3e6-d540-430b-a378-7956fdae1b6d","Predicate":{"Uid":"a4a809af-ae1d-4b98-8427-2fd3b6de412a","Name":"from community simple example","Inverse":"from community inverse","Type":"Simple","IsSystem":true,"IsInUse":false},"Segments":[{"Value":"08 Test"}]}]`;
        this.sourceAssets = JSON.parse(json);
    }

    prepopulateSearchResult() {
        var json = `[{"AssetTypeUid":"","Uid":"","Segments":[{"Value":"AzureRemoteHost"},{"Value":"EnrolDB"},{"Value":"SSMS"},{"Value":"MEMBER_INFO"},{"Value":"Name"}],"IsSelected":false},{"AssetTypeUid":"","Uid":"","Segments":[{"Value":"OracleHost"},{"Value":"ClaimsDb"},{"Value":"dbo"},{"Value":"MEMBERS"},{"Value":"MEMBER_NAME"}],"IsSelected":false},{"AssetTypeUid":"","Uid":"","Segments":[{"Value":"Data Warehouse"},{"Value":"DWDB"},{"Value":"edm"},{"Value":"MEMBERS"},{"Value":"MEMBER_NAME"}],"IsSelected":false}]`;
        this.sourcePrePop = JSON.parse(json);
    }

    onAssetSearchSelection(event: any) {
        console.warn("Search selection event triggered!");
        console.warn("Event:", event);
    }

}


