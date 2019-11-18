import { Component, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { CommonComponentAssetResult, CommonComponentAssetTypeFilter } from '../../../../models/asset-search.model';
import { PredicateType } from '../../../../models/predicate.model';
import { AssetTypeClass } from '../../../../models/asset.model';
import { AssetService } from '../../../../services/asset.service';


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
    private isAddTransformationVisible: boolean = false;

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
        var json = `[{"AssetTypeUid":"527ff749-fc47-4356-92aa-67f58a73a1af","Uid":"0267efb9-84c3-4371-9b13-18902fdbfc6b","Predicate":null,"Segments":[{"Value":"Shyam adding a new artifact to test delete workflow"}]},{"AssetTypeUid":"9af94c0a-cd70-4246-95a8-840cc6d6fec3","Uid":"086ca61b-ebee-4c48-a296-fe2fb2cf1989","Predicate":null,"Segments":[{"Value":"(Sme SCHEMA"},{"Value":"(Sme function) (Shyam testing) how Parens will work ()"}]},{"AssetTypeUid":"9af94c0a-cd70-4246-95a8-840cc6d6fec3","Uid":"a42d9455-e3b5-4627-adeb-1175c60f6c5f","Predicate":null,"Segments":[{"Value":"(Sme SCHEMA"},{"Value":"(Sme function) (Shyam testing) how Parens will work 2 ()"}]},{"AssetTypeUid":"99fd5ad4-ec9f-4d0e-84bd-0539b2fb3d37","Uid":"e9d15314-ed4f-46e2-9ccf-d049c3c91209","Predicate":null,"Segments":[{"Value":"(Sme SCHEMA"},{"Value":"(Sme function) (Shyam testing) how Parens will work 2 ()"},{"Value":"(Shyam testing) how Parens will work2 ()"}]}]`;
        this.sourceAssets = JSON.parse(json);
    }

    prepopulateSearchResult() {
        var json = `[{"Uid":"d46d09c9-6657-4a1d-bdeb-1f42976d5d3e","AssetTypeUid":"8a4c2c8e-29cc-441b-a03c-addd7d0e94b6","Segments":[{"Value":"CADIS"},{"Value":"IL_MAPPED_DIV_FQCY"},{"Value":"Portia Dividend Mode"}]},{"Uid":"d6c9de11-d0b3-426f-bcdf-870f872626ea","AssetTypeUid":"e9a2dbfd-d9ce-466d-ae57-1004db33a2fa","Segments":[{"Value":"CADIS_PROC"},{"Value":"DC_CRPREMASTE_BLMAUTOBLD_PREP"},{"Value":"Is Muni AdJ Coupon Mode pop?"}]},{"Uid":"c80ce2cf-2509-4860-9b9c-12d831eb19bc","AssetTypeUid":"e9a2dbfd-d9ce-466d-ae57-1004db33a2fa","Segments":[{"Value":"CADIS_PROC"},{"Value":"DC_CRPREMASTE_BLMAUTOBLD_PRLIM"},{"Value":"Is Muni AdJ Coupon Mode pop?"}]},{"Uid":"e4b8c058-21d1-465d-976c-0ad1d479179e","AssetTypeUid":"6df437a9-574c-4754-acad-1cd67098d616","Segments":[{"Value":"MAPPED_DIV_FQCY"},{"Value":"Portia Dividend Mode"}]},{"Uid":"6219b146-1e07-4375-84e3-7dd670b63525","AssetTypeUid":"a8b2e96a-8c83-4f6b-a6e8-708741c9a0f0","Segments":[{"Value":"SYS"},{"Value":"V_$DIAG_HM_RUN"},{"Value":"MODE"}]},{"Uid":"962a8038-35a3-4156-8d2c-027ff671f6a6","AssetTypeUid":"a8b2e96a-8c83-4f6b-a6e8-708741c9a0f0","Segments":[{"Value":"SYS"},{"Value":"V_$DIAG_HM_RUN"},{"Value":"MODE"}]}]`;
        this.sourcePrePop = JSON.parse(json);
    }

    onAssetSearchSelection(event: any) {
        console.warn("Search selection event triggered!");
        console.warn("Event:", event);
    }

    newAssetAdded($event) {
        var item = new CommonComponentAssetResult();
        item.Uid = $event.assetUid;
        item.AssetTypeUid = $event.assetTypeUid;

        let arr = [];
        arr.push(item);

        this.transformationAsset = arr;
        this.isAddTransformationVisible = false;
    }

    onCancel() {
        this.isAddTransformationVisible = false;
    }

}


