import { Component, ChangeDetectionStrategy, OnInit, HostBinding, Input, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonComponentAssetResult, CommonComponentAssetTypeFilter, CommonComponentAssetTypeFilterSideOfRelationship, CommonComponentAssetTypeFilterRelationshipSide } from '../../../../models/asset-search.model';
import { PredicateType } from '../../../../models/predicate.model';
import { AssetTypeClass } from '../../../../models/asset.model';
import { AssetService } from '../../../../services/asset.service';
import { RelationshipsService } from '../../../../services/relationships.service';
import { Observable, forkJoin } from 'rxjs';
import { exec } from 'child_process';

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
    providers: [RelationshipsService],
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

    private transformationRelationships: any[] = [];

    private sourcePrePop: CommonComponentAssetResult[] = [];
    private isAddTransformationVisible: boolean = false;

    private predicateType: PredicateType = PredicateType.Simple;
    private showPredicateSelector: boolean = false;


    private topWarningMessage: string = '';
    private bottomWarningMessage: string = '';

    private isTransformationDisabled: boolean = true;
    private isTargetDisabled: boolean = true;

    private isSaving: boolean = false;
    private isSavingAndContinue: boolean = false;
    private afterSaveEvent: Function;

    constructor(
        private relationshipService: RelationshipsService,
        private ref: ChangeDetectorRef
    ) { }

    ngOnInit() {

        this.loadSettings();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.selected.previousValue != changes.selected.currentValue) {
            this.loadSettings();
        }

        this.checkSelectionValues();
    }

    private checkSelectionValues() {
        if (this.transformationAsset.length > 0) {
            var transformationUid = this.transformationAsset[0].AssetTypeUid;
            var objectUid = this.transformationRelationships.find(x => x.SubjectUid == transformationUid).ObjectUid;
            var tf = new CommonComponentAssetTypeFilter();
            tf.Uid = objectUid;
            this.targetFilters.push(tf);
            this.isTargetDisabled = false;
        }

        if (this.sourceAssets.length > 0) {
            this.isTransformationDisabled = false;
        }

    }

    clearArr(arr: any[]) {
        arr = JSON.parse(JSON.stringify(arr));
    }

    private loadSettings() {
        this.clearArr(this.sourceAssets);
        this.clearArr(this.transformationFilters);
        this.clearArr(this.targetAssets);

        if (this.editorType == RelationshipEditorType.Lineage) {
            this.predicateType = PredicateType.DataLineage;
        }
        else {
            this.showPredicateSelector = true;
        }
        var sf = new CommonComponentAssetTypeFilter();
        sf.Uid = this.selected.Uid;
        this.sourceFilters.push(sf);

        this.relationshipService.getTransformationRelationship(this.selected.Uid)
            .subscribe(x => {
                this.transformationRelationships = x;
                this.transformationRelationships.forEach(tr => {
                    if (tr.SubjectUid == this.selected.Uid) {
                        var tf = new CommonComponentAssetTypeFilter();
                        tf.UseAsTransformation = true;
                        tf.Uid = tr.ObjectUid;
                        this.transformationFilters.push(tf);
                    }
                });
                this.ref.markForCheck();
            });

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
        console.warn("Event:", event);
        this.checkSelectionValues();
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
        if (this.isSaving || this.isSavingAndContinue)
            return false;

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

    private saveAndContinue() {
        this.isSavingAndContinue = true;
        this.afterSaveEvent = function () {
            this.selected.Uid = this.targetAssets[0].AssetTypeUid;
            this.loadSettings();
        };
        this.executeSave();
    }

    private save() {
        this.isSaving = true;
        this.afterSaveEvent = function () {
            this.loadSettings();
        };
        this.executeSave();

    }

    private executeSave() {
        var relationships = this.buildRelationshipsFromSelection();

        if (relationships.length > 0) {
            var tasks = [];
            relationships.forEach(r => {
                tasks.push(this.relationshipService.saveRelationships(r.IntersectTypeUid, r.Intersects));
            })
            var insertObs = forkJoin(tasks);
            insertObs.subscribe(results => {
                this.processResults(results);
                this.afterSaveEvent();
            });
        }
    }

    private processResults(results: any[]) {
        this.isSaving = this.isSavingAndContinue = false;

        var successfull = results.filter(x => x.Success == true);
        var failed = results.filter(x => x.Success == false);
        console.log(results);

        if (failed.length > 0) {
            this.bottomWarningMessage += "Relationship not created";
        }

        var existing = successfull.filter(x => x.IsNew == false);

        existing.forEach(x => {
            this.bottomWarningMessage += x.Id + " relationship already exist.Not created!";

        });

        this.ref.markForCheck();
    }

    buildRelationshipsFromSelection(): any[] {
        var relationships = [];
        if (this.editorType == RelationshipEditorType.Lineage) {
            var rel1Uid = this.transformationRelationships.find(x => x.SubjectUid == this.sourceAssets[0].AssetTypeUid && x.ObjectUid == this.transformationAsset[0].AssetTypeUid).IntersectTypeUid;
            var rel2Uid = this.transformationRelationships.find(x => x.ObjectUid == this.targetAssets[0].AssetTypeUid && x.SubjectUid == this.transformationAsset[0].AssetTypeUid).IntersectTypeUid;

            var transformation = this.transformationAsset[0];

            var rel1: any = {};
            rel1.IntersectTypeUid = rel1Uid;
            rel1.Intersects = [];
            this.sourceAssets.forEach(a => {
                rel1.Intersects.push({ SubjectAssetUid: a.Uid, ObjectAssetUid: transformation.Uid });
            });

            var rel2: any = {};
            rel2.IntersectTypeUid = rel2Uid;
            rel2.Intersects = [];
            this.targetAssets.forEach(a => {
                rel2.Intersects.push({ ObjectAssetUid: a.Uid, SubjectAssetUid: transformation.Uid });
            });

            relationships.push(rel1);
            relationships.push(rel2);
        }
        return relationships;
    }

}


