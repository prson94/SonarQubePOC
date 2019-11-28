import { Component, ChangeDetectionStrategy, OnInit, HostBinding, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonComponentAssetResult, CommonComponentAssetTypeFilter, CommonComponentAssetTypeFilterSideOfRelationship, CommonComponentAssetTypeFilterRelationshipSide } from '../../../../models/asset-search.model';
import { PredicateType } from '../../../../models/predicate.model';
import { AssetTypeClass } from '../../../../models/asset.model';
import { AssetService } from '../../../../services/asset.service';

declare var CompanySettings;
export enum RelationshipEditorType {
    Lineage = 'Lineage',
    RelatedAssets = 'RelatedAssets'
}

export class RelationshipInsertModel {
    IntersectTypeUid: string;
    SubjectUid: string;
    ObjectUid: string;
    IsTypeResolved: boolean = false;
    IsSaved: boolean = false;
}

@Component({
    selector: 'd3s-diagram-relationships',
    templateUrl: 'relationships.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DiagramAssetRelationshipComponent implements OnInit, OnChanges {
    @HostBinding('class') class = 'relationship-editor';

    @Input() selected: any;

    private editorType: RelationshipEditorType = RelationshipEditorType.Lineage;
    private sourceAssets: CommonComponentAssetResult[] = [];
    private targetAssets: CommonComponentAssetResult[] = [];
    private transformationAsset: CommonComponentAssetResult[] = [];

    private transformationFilters: CommonComponentAssetTypeFilter[] = [];
    private sourceFilters: CommonComponentAssetTypeFilter[] = [];
    private targetFilters: CommonComponentAssetTypeFilter[] = [];

    private sourcePrePop: CommonComponentAssetResult[] = [];
    private isAddTransformationVisible: boolean = false;

    private predicateType: PredicateType = PredicateType.Simple;
    private showPredicateSelector: boolean = false;


    private topWarningMessage: string = '';
    private bottomWarningMessage: string = '';

    constructor() { }

    ngOnInit() {

        this.loadSettings();
        console.log(this.selected);
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.selected.previousValue != changes.selected.currentValue) {
            this.loadSettings();
        }
    }
    private loadSettings() {
        if (this.editorType == RelationshipEditorType.Lineage) {
            this.predicateType = PredicateType.DataLineage;
        }
        else {
            this.showPredicateSelector = true;
        }
        var sf = new CommonComponentAssetTypeFilter();
        sf.Uid = this.selected.Uid;
        this.sourceFilters.push(sf);

        var tf = new CommonComponentAssetTypeFilter();
        tf.UseAsTransformation = true;
        this.transformationFilters.push(tf);
    }

    private changeEditorType(type: RelationshipEditorType) {
        if (this.sourceAssets.length > 0 || this.targetAssets.length > 0) {
            this.topWarningMessage = 'You cannot switch! Save your changes or remove selection from Source and Target assets';
        }
        else {
            this.topWarningMessage = '';
            this.editorType = type;
            this.loadSettings();
        }
    }

    onAssetSearchSelection(event: any) {
        console.warn("Search selection event triggered!");
        console.warn("Event:", event);
        this.resolveAssets();
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

    private get IsValid(): boolean {
        if (this.sourceAssets.length > 0 && this.transformationAsset.length > 0 && this.targetAssets.length > 0) {
            return true;
        }
        return false;
    }

    resolveAssets() {
        if (this.sourceAssets.length > 0 && this.transformationAsset.length > 0) {
            var transformAsset = this.transformationAsset[0];

        }
    }

    private save() {

    }

}


