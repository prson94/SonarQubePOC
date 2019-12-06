import { Component, EventEmitter, ChangeDetectionStrategy, OnInit, HostBinding, Input, OnChanges, SimpleChanges, ChangeDetectorRef, Output } from '@angular/core';
import { CommonComponentAssetResult, CommonComponentAssetTypeFilter, CommonComponentAssetTypeFilterSideOfRelationship, CommonComponentAssetTypeFilterRelationshipSide, CommonComponentAssetResultExt, CommonComponentAssetSelection } from '../../../../models/asset-search.model';
import { PredicateType } from '../../../../models/predicate.model';
import { RelationshipsService } from '../../../../services/relationships.service';
import { Observable, forkJoin } from 'rxjs';
import { Predicate } from '../../../../models/predicate.model';
import { delay, take } from 'rxjs/operators';

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

    @Output() refreshDiagram: EventEmitter<any> = new EventEmitter();

    @Input() assetBrowserData: any;
    private editorType: RelationshipEditorType = RelationshipEditorType.Lineage;
    private sourceAssets: CommonComponentAssetSelection[] = [];
    private targetAssets: CommonComponentAssetSelection[] = [];
    private transformationAsset: CommonComponentAssetSelection[] = [];

    private transformationFilters: CommonComponentAssetTypeFilter[] = [];
    private sourceFilters: CommonComponentAssetTypeFilter[] = [];
    private targetFilters: CommonComponentAssetTypeFilter[] = [];

    private targetAllowedPredicates: Predicate[] = [];
    private transformationRelationships: any[] = [];

    private sourcePrePop: CommonComponentAssetResult[] = [];
    private isAddTransformationVisible: boolean = false;

    private predicateType: PredicateType = PredicateType.Transformation;
    private showPredicateSelector: boolean = false;

    private topWarningMessage: string = '';
    private bottomWarningMessage: string = '';

    private isTransformationDisabled: boolean = true;
    private isTargetDisabled: boolean = true;

    private isSaving: boolean = false;
    private isSavingAndContinue: boolean = false;
    private afterSaveEvent: Function;

    private relationshipsError: any[] = [];
    private areRelationshipsValid = false;
    private areAllItemsSelected = false;

    private noAssetOnDiagram: boolean = false;

    constructor(
        private relationshipService: RelationshipsService,
        private ref: ChangeDetectorRef
    ) { }

    ngOnInit() {
        this.loadSettings(false);

        if (this.assetBrowserData && this.assetBrowserData.assets) {
            var tempArr = [];
            this.assetBrowserData.assets.filter(x => x.useAsTransformation == false).forEach(group => {
                group.items.forEach(asset => {
                    if (this.sourcePrePop.length < 10) {
                        var item = new CommonComponentAssetResult();
                        item.AssetTypeUid = group.assetUid;
                        item.Uid = asset.assetUid;
                        tempArr.push(item);
                    }
                });
            });
            this.sourcePrePop = tempArr;
        }
        this.ref.markForCheck();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.checkSelectionValues();
    }

    private checkSelectionValues() {
        if (this.transformationAsset.length > 0) {
            this.relationshipService.getRelationshipsByAssetTypeUid(this.transformationAsset[0].AssetTypeUid)
                .subscribe(res => {
                    this.targetAllowedPredicates = [];
                    res.forEach(rel => {
                        if (rel.Predicate.Type == PredicateType.Transformation.toString()) {
                            this.targetAllowedPredicates.push(rel.Predicate);
                        }
                    });
                    this.buildTargetFilters();
                    this.isTargetDisabled = false;
                });
        }

        if (this.sourceAssets.length > 0) {
            this.isTransformationDisabled = false;
        }
        this.noAssetOnDiagram = false;
        this.areAllItemsSelected = false;

        if (this.sourceAssets.length > 0 && this.transformationAsset.length > 0 && this.targetAssets.length) {
            this.areAllItemsSelected = true;
            var doesSourceContains = false;
            var doesTargetContains = false;

            this.assetBrowserData.assets.forEach(type => {
                type.items.forEach(asset => {
                    this.sourceAssets.forEach(sa => {
                        if (sa.Uid == asset.assetUid)
                            doesSourceContains = true;
                    });
                    this.targetAssets.forEach(sa => {
                        if (sa.Uid == asset.assetUid)
                            doesTargetContains = true;
                    });
                });
            });
            if (!doesSourceContains && !doesTargetContains) {
                this.noAssetOnDiagram = true;
            }
        }
    }

    private loadSettings(switchTargetToSource: boolean) {
        var tempSource = JSON.parse(JSON.stringify(this.targetAssets));

        this.sourceAssets = [];
        this.transformationAsset = [];
        this.targetAssets = [];

        if (switchTargetToSource == true && tempSource.length > 0) {
            tempSource.forEach(x => x.Predicate = null);
            this.sourceAssets = tempSource;
        }

        if (this.editorType == RelationshipEditorType.Lineage) {
            this.predicateType = PredicateType.Transformation;
        }

        if (this.editorType == RelationshipEditorType.Lineage) {
            this.buildSourceFilters();
            this.buildTransformationFilters();
            this.buildTargetFilters();
        }
    }
    private buildTargetFilters() {
        this.targetFilters = [];
        if (this.targetAllowedPredicates.length == 0) {
            var targetFilters = new CommonComponentAssetTypeFilter();
            targetFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
            targetFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object;
            targetFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
            this.targetFilters.push(targetFilters);
        }
        else {
            this.targetAllowedPredicates.forEach(tp => {
                var targetFilters = new CommonComponentAssetTypeFilter();
                targetFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
                targetFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object;
                targetFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
                targetFilters.AsSideOfRelationship.PredicateUid = tp.Uid.toString();
                this.targetFilters.push(targetFilters);
            });
        }
    }

    private buildSourceFilters() {
        this.sourceFilters = [];
        if (this.sourceFilters.length == 0) {
            var sourceFilters = new CommonComponentAssetTypeFilter();
            sourceFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
            sourceFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Subject;
            sourceFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
            this.sourceFilters.push(sourceFilters);
        }
        else {
            this.sourceAssets.forEach(asset => {
                var sourceFilters = new CommonComponentAssetTypeFilter();
                sourceFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
                sourceFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Subject;
                if (asset.Predicate)
                    sourceFilters.AsSideOfRelationship.PredicateUid = asset.Predicate.Uid.toString();
                sourceFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
                this.sourceFilters.push(sourceFilters);
            });
        }
    }

    private buildTransformationFilters() {
        this.transformationFilters = [];
        if (this.sourceAssets.length == 0) {
            var transformationFilters = new CommonComponentAssetTypeFilter();
            transformationFilters.UseAsTransformation = true;
            transformationFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
            transformationFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
            transformationFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object
            this.transformationFilters.push(transformationFilters);
        }
        else {
            this.sourceAssets.forEach(asset => {
                var transformationFilters = new CommonComponentAssetTypeFilter();
                transformationFilters.UseAsTransformation = true;
                transformationFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
                transformationFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
                if (asset.Predicate)
                    transformationFilters.AsSideOfRelationship.PredicateUid = asset.Predicate.Uid.toString();
                transformationFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object
                this.transformationFilters.push(transformationFilters);
            });
        }
    }

    private changeEditorType(type: RelationshipEditorType) {
        if (this.sourceAssets.length > 0 || this.targetAssets.length > 0) {
            this.topWarningMessage = 'You cannot switch! Save your changes or remove selection from Source and Target assets';
        }
        else {
            this.topWarningMessage = '';
            this.editorType = type;
            this.loadSettings(false);
        }
    }

    onAssetSearchSelection(event: any) {
        this.checkSelectionValues();
        this.buildTransformationFilters();
        this.buildSourceFilters();
        this.buildTargetFilters();

        this.validateRelationships();

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

        if (!this.areRelationshipsValid)
            return false;

        if (this.isSaving || this.isSavingAndContinue)
            return false;

        if (this.editorType == RelationshipEditorType.Lineage && this.sourceAssets.length > 0 && this.transformationAsset.length > 0 && this.targetAssets.length > 0) {
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
        this.afterSaveEvent = function (ev: boolean) {
            if (ev) {
                this.loadSettings(true);
                this.refreshDiagram.emit();
            }
            this.isSaving = this.isSavingAndContinue = false;
        };
        this.executeSave();
    }

    private save() {
        this.isSaving = true;
        this.afterSaveEvent = function (ev: boolean) {
            if (ev) {
                this.loadSettings(false);
                this.refreshDiagram.emit();
            }
            this.isSaving = this.isSavingAndContinue = false;
        };
        this.executeSave();

    }

    private validateRelationships() {
        this.areRelationshipsValid = false;
        this.relationshipsError = [];

        this.sourceAssets.forEach(x => {
            x.Warnings = [];
            if (!x.Predicate) {
                x.Warnings.push("Predicate not set!");
            }
        });
        this.targetAssets.forEach(x => {
            x.Warnings = [];

            if (!x.Predicate) {
                x.Warnings.push("Predicate not set!");
            }
        });

        var relationships = this.buildRelationshipsFromSelection();

        var resolveRelationshipTasks = [];
        relationships.forEach(r => {
            resolveRelationshipTasks.push(this.relationshipService.getRelationshipsByAssetTypeUid(r.SubjectAssetTypeUid));
        });

        var resolveRelationshipsObservable = forkJoin(resolveRelationshipTasks);
        resolveRelationshipsObservable.subscribe(results => {
            var eligibleRelationships = [];
            results.forEach(res => {
                res.forEach(r => {
                    if (r.Predicate.Type == 'Transformation') {
                        eligibleRelationships.push(r);
                    }
                });
            });

            relationships.forEach(rel => {
                var intersectType = eligibleRelationships.find(x => x.Predicate.Uid == rel.PredicateUid && x.Object.Uid == rel.ObjectAssetTypeUid && x.Subject.Uid == rel.SubjectAssetTypeUid);
                rel.IntersectTypeUid = intersectType ? intersectType.Uid : null;
            });


            var invalidRelationships = relationships.filter(x => x.IntersectTypeUid == null);
            invalidRelationships.forEach(inv => {
                inv.Intersects.forEach(rel => {
                    this.sourceAssets.forEach(sa => {
                        if (sa.Predicate && inv.PredicateUid == sa.Predicate.Uid.toString() && sa.Uid == rel.SubjectAssetUid) {
                            sa.Warnings = [];
                            sa.Warnings.push("Cannot create relationship of this type!");
                        }
                    });
                    this.targetAssets.forEach(ta => {

                        if (ta.Predicate && inv.PredicateUid == ta.Predicate.Uid.toString() && ta.Uid == rel.ObjectAssetUid) {
                            ta.Warnings = [];
                            ta.Warnings.push("Cannot create relationship of this type!");
                        }
                    });
                });
            });

            if (invalidRelationships.length == 0) {
                this.areRelationshipsValid = true;
            }
        });

    }

    private executeSave() {
        var relationships = this.buildRelationshipsFromSelection();

        var resolveRelationshipTasks = [];
        relationships.forEach(r => {
            resolveRelationshipTasks.push(this.relationshipService.getRelationshipsByAssetTypeUid(r.SubjectAssetTypeUid));
        });

        var resolveRelationshipsObservable = forkJoin(resolveRelationshipTasks);
        resolveRelationshipsObservable.subscribe(results => {
            var eligibleRelationships = [];
            results.forEach(res => {
                res.forEach(r => {
                    if (r.Predicate.Type == 'Transformation') {
                        eligibleRelationships.push(r);
                    }
                });
            });

            relationships.forEach(rel => {
                var intersectType = eligibleRelationships.find(x => x.Predicate.Uid == rel.PredicateUid && x.Object.Uid == rel.ObjectAssetTypeUid && x.Subject.Uid == rel.SubjectAssetTypeUid);
                rel.IntersectTypeUid = intersectType ? intersectType.Uid : null;
            });

            if (!relationships.some(x => x.IntersectTypeUid == null)) {
                this.postRelationships(relationships);
            }
            else {
                this.afterSaveEvent(false);
                relationships.filter(x => x.IntersectTypeUid == null).forEach(fail => {
                    var errorMsg = 'This lineage relationship cannot be created, as there is no relationship type defined between 2 asset types:';

                    var subjectTitle = 'Source Asset:';
                    var objectTitle = 'Transformation:';
                    if (fail.type == 'T->S') {
                        subjectTitle = objectTitle;
                        objectTitle = 'Target Asset:';
                    }
                    var subject = this.getAssetFromSelection(fail.Intersects[0].SubjectAssetUid);
                    var object = this.getAssetFromSelection(fail.Intersects[0].ObjectAssetUid);
                    this.relationshipsError.push({ errorMsg, subject, subjectTitle, object, objectTitle });
                });
            }
        });
    }

    private postRelationships(relationships: any[]) {
        var tasks = [];
        relationships.forEach(r => {
            tasks.push(this.relationshipService.saveRelationshipsForked(r.IntersectTypeUid, r.Intersects));
        })
        var insertObs = forkJoin(tasks);
        insertObs.subscribe(results => {
            var isSuccess = this.processResults(results);
            this.afterSaveEvent(isSuccess);
        });
    }

    private processResults(results: any[]): boolean {
        let rollback: boolean = false;
        this.relationshipsError = [];
        results.forEach(res => {
            var data = res.obj;
            var result: any[] = res.response;
            result.forEach((r, idx) => {
                if (r.Success == false) {
                    
                    var errorMsg = r.Message;
                    rollback = true;

                    var subjectTitle = 'Source Asset:';
                    var objectTitle = 'Transformation:';
                    if (data.model[idx].type == 'T->S') {
                        subjectTitle = objectTitle;
                        objectTitle = 'Target Asset:';
                    }
                    var subject = this.getAssetFromSelection(data.model[idx].SubjectAssetUid);
                    var object = this.getAssetFromSelection(data.model[idx].ObjectAssetUid);
                    this.relationshipsError.push({ errorMsg, subject, subjectTitle, object, objectTitle });
                    console.log(this.relationshipsError);
                }

            });
        })

        //If error occured, delete only newly created relationships
        if (rollback) {
            var deleteTasks = [];
            results.forEach(res => {
                var ituid = res.obj.intersectTypeUid;
                let rels: any[] = [];
                var arr = <any[]>res.response;
                arr.forEach(rel => {
                    if (rel.IsNew == true) {
                        rels.push({ uid: rel.uid });
                    }
                });
                deleteTasks.push(this.relationshipService.deleteRelationshipV2(ituid, rels));
            });

            var insertObs = forkJoin(deleteTasks);
            insertObs.subscribe(results => {
                console.log(results);
            });
            this.ref.markForCheck();

            return false;
        }
        this.ref.markForCheck();
        return true;

    }

    buildRelationshipsFromSelection(): any[] {
        var relationships = [];
        if (this.editorType == RelationshipEditorType.Lineage) {
            var transformation = this.transformationAsset[0];

            if (this.transformationAsset.length != 0) {
                this.sourceAssets.forEach(a => {
                    var rel1: any = {};
                    rel1.Intersects = [];
                    rel1.SubjectAssetTypeUid = a.AssetTypeUid;
                    rel1.ObjectAssetTypeUid = transformation.AssetTypeUid;
                    if (a.Predicate)
                        rel1.PredicateUid = a.Predicate.Uid;
                    else rel1.PredicateUid = '';
                    rel1.Intersects.push({ SubjectAssetUid: a.Uid, ObjectAssetUid: transformation.Uid, type: 'S->T' });
                    relationships.push(rel1);
                });
            }

            this.targetAssets.forEach(a => {
                var rel2: any = {};
                rel2.Intersects = [];
                rel2.ObjectAssetTypeUid = a.AssetTypeUid;
                if (a.Predicate)
                    rel2.PredicateUid = a.Predicate.Uid;
                else rel2.PredicateUid = '';
                rel2.SubjectAssetTypeUid = transformation.AssetTypeUid;
                rel2.Intersects.push({ ObjectAssetUid: a.Uid, SubjectAssetUid: transformation.Uid, type: 'T->S' });
                relationships.push(rel2);
            });

        }
        return relationships;
    }


    private getAssetFromSelection(assetUid) {
        let result: CommonComponentAssetResult;
        result = this.sourceAssets.find(x => x.Uid == assetUid);
        if (result === undefined)
            result = this.transformationAsset.find(x => x.Uid == assetUid);

        if (result === undefined)
            result = this.targetAssets.find(x => x.Uid == assetUid);

        return result;
    }

}


