import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    HostBinding,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges
} from '@angular/core';
import { forkJoin } from 'rxjs';
import { RelationshipsService } from '../../../../../services/relationships.service';
import { AssetBrowserResponseModel, AssetBrowserTranslationNode } from '../../../../../models/lineage.model';
import {
    CommonComponentAssetResult,
    CommonComponentAssetSelection,
    CommonComponentAssetTypeFilter,
    CommonComponentAssetTypeFilterRelationshipSide,
    CommonComponentAssetTypeFilterSideOfRelationship
} from '../../../../../models/asset-search.model';
import { Predicate, PredicateType } from '../../../../../models/predicate.model';

export enum RelationshipEditorType {
    Lineage = 'Lineage',
    RelatedAssets = 'RelatedAssets'
}

@Component({
    selector: 'd3s-assetbrowser-addpanel',
    templateUrl: 'addpanel.component.html',
    providers: [RelationshipsService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserAddPanelComponent implements OnInit, OnChanges {
    @HostBinding('class') class = 'relationship-editor';

    @Output() refreshDiagram: EventEmitter<any> = new EventEmitter();

    @Input() assetBrowserData: AssetBrowserResponseModel;

    private readonly emptyUid: string = '00000000-0000-0000-0000-000000000000';
    private browserAssets: CommonComponentAssetResult[] = [];
    editorType: RelationshipEditorType = RelationshipEditorType.Lineage;
    RelationshipEditorType = RelationshipEditorType;
    sourceAssets: CommonComponentAssetSelection[] = [];
    targetAssets: CommonComponentAssetSelection[] = [];
    transformationAsset: CommonComponentAssetSelection[] = [];

    transformationFilters: CommonComponentAssetTypeFilter[] = [];
    sourceFilters: CommonComponentAssetTypeFilter[] = [];
    targetFilters: CommonComponentAssetTypeFilter[] = [];

    targetAllowedPredicates: Predicate[] = [];
    transformationRelationships: any[] = [];

    sourcePrePop: CommonComponentAssetResult[] = [];
    isAddTransformationVisible: boolean = false;

    predicateType: PredicateType = PredicateType.Transformation;
    showPredicateSelector: boolean = false;

    topWarningMessage: string = '';
    bottomWarningMessage: string = '';

    isTransformationDisabled: boolean = true;
    isTargetDisabled: boolean = true;

    isSaving: boolean = false;
    isSavingAndContinue: boolean = false;
    afterSaveEvent: Function;

    relationshipsError: any[] = [];
    areRelationshipsValid = false;
    areAllItemsSelected = false;

    noAssetOnDiagram: boolean = false;

    sourceBtnText: string = $localize`Add source asset`;
    targetBtnText: string = $localize`Add target asset`;

    missingPredicateSource: boolean = false;
    missingPredicateTarget: boolean = false;

    helpTextTop: number = 0;

    constructor(
        private relationshipService: RelationshipsService,
        private ref: ChangeDetectorRef
    ) { }

    ngOnInit() {
        this.loadSettings(false);
        if (this.assetBrowserData && this.assetBrowserData.nodes) {
            this.assetBrowserData.nodes.forEach((a) => {
                    this.populateAssets(a);
            });

            const sourceItems = this.browserAssets.filter((x) => x["isSubjectInTransformation"] == true);

            if (this.browserAssets.length > 10)
                {this.sourcePrePop = sourceItems.slice(0, 10);}
            else {this.sourcePrePop = sourceItems;}
        }
        this.ref.markForCheck();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.checkSelectionValues();
        this.validateRelationships();
        if (changes.assetBrowserData.currentValue != changes.assetBrowserData.previousValue && this.assetBrowserData) {
            this.assetBrowserData.nodes.forEach((a) => {
                this.populateAssets(a);
            });
        }
    }

    private populateAssets(node: AssetBrowserTranslationNode) {
        if (node.class && node.assetUid && node.assetUid !== this.emptyUid) {
            const item = new CommonComponentAssetResult();
            item.AssetTypeUid = node.assetTypeUid;
            item.AssetTypeIcon = node.icon; 
            item.AssetTypeName = node.class.toString();
            item.Uid = node.assetUid;
            if (node.useAsTransformation == false && !this.browserAssets.find((x) => x.Uid == item.Uid && x.AssetTypeUid == item.AssetTypeUid)) {
                item["isSubjectInTransformation"] = node.isSubjectInTransformation;
                this.browserAssets.push(item);
            }
        }
    }

    private checkSelectionValues() {
        if (this.transformationAsset.length > 0) {
            this.isTargetDisabled = false;
            this.relationshipService.getRelationshipsByAssetTypeUid(this.transformationAsset[0].AssetTypeUid)
                .subscribe((res) => {
                    this.targetAllowedPredicates = [];
                    res.forEach((rel) => {
                        if (rel.Predicate.Type == PredicateType.Transformation.toString()) {
                            this.targetAllowedPredicates.push(rel.Predicate);
                        }
                    });
                    this.buildTargetFilters();
                });
        }
        else {
            this.isTargetDisabled = true;
        }

        if (this.sourceAssets.length > 0) {
            this.isTransformationDisabled = false;
        }
        else {
            this.isTransformationDisabled = true;
            this.isTargetDisabled = true;
        }


        this.noAssetOnDiagram = false;
        this.areAllItemsSelected = false;

        if (this.sourceAssets.length > 0 && this.transformationAsset.length > 0 && this.targetAssets.length) {
            let doesSourceContains = false;
            let doesTargetContains = false;

            this.browserAssets.forEach((asset) => {
                this.sourceAssets.forEach((sa) => {
                    if (sa.Uid == asset.Uid)
                        {doesSourceContains = true;}
                });
                this.targetAssets.forEach((sa) => {
                    if (sa.Uid == asset.Uid)
                        {doesTargetContains = true;}
                });
            });

            if (!doesSourceContains && !doesTargetContains) {
                this.noAssetOnDiagram = true;
            }

        }
    }

    private loadSettings(switchTargetToSource: boolean) {
        const tempSource = JSON.parse(JSON.stringify(this.targetAssets));

        this.sourceAssets = [];
        this.transformationAsset = [];
        this.targetAssets = [];

        if (tempSource) {
            if (switchTargetToSource == true && tempSource.length > 0) { 
                tempSource.forEach((x) => x.Predicate = null);
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
    }

    private buildTargetFilters() {
        this.targetFilters = [];
        if (this.targetAllowedPredicates.length == 0) {
            const targetFilters = new CommonComponentAssetTypeFilter();
            targetFilters.UseAsTransformation = false;
            targetFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
            targetFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object;
            targetFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
            this.targetFilters.push(targetFilters);
        }
        else {
            this.targetAllowedPredicates.forEach((tp) => {
                const targetFilters = new CommonComponentAssetTypeFilter();
                targetFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
                targetFilters.UseAsTransformation = false;
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
            const sourceFilters = new CommonComponentAssetTypeFilter();
            sourceFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
            sourceFilters.UseAsTransformation = false;
            sourceFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Subject;
            sourceFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
            this.sourceFilters.push(sourceFilters);
        }
        else {
            this.sourceAssets.forEach((asset) => {
                const sourceFilters = new CommonComponentAssetTypeFilter();
                sourceFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
                sourceFilters.UseAsTransformation = false;
                sourceFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Subject;
                if (asset.Predicate)
                    {sourceFilters.AsSideOfRelationship.PredicateUid = asset.Predicate.Uid.toString();}
                sourceFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
                this.sourceFilters.push(sourceFilters);
            });
        }
    }

    private buildTransformationFilters() {
        this.transformationFilters = [];
        if (this.sourceAssets.length == 0) {
            const transformationFilters = new CommonComponentAssetTypeFilter();
            transformationFilters.UseAsTransformation = true;
            transformationFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
            transformationFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
            transformationFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object;
            this.transformationFilters.push(transformationFilters);
        }
        else {
            this.sourceAssets.forEach((asset) => {
                const transformationFilters = new CommonComponentAssetTypeFilter();
                transformationFilters.UseAsTransformation = true;
                transformationFilters.AsSideOfRelationship = new CommonComponentAssetTypeFilterSideOfRelationship();
                transformationFilters.AsSideOfRelationship.PredicateType = PredicateType.Transformation;
                if (asset.Predicate)
                    {transformationFilters.AsSideOfRelationship.PredicateUid = asset.Predicate.Uid.toString();}
                transformationFilters.AsSideOfRelationship.Side = CommonComponentAssetTypeFilterRelationshipSide.Object;
                this.transformationFilters.push(transformationFilters);
            });
        }
    }

    changeEditorType(type: RelationshipEditorType) {
        if (this.sourceAssets.length > 0 || this.targetAssets.length > 0) {
            this.topWarningMessage = $localize`You cannot switch! Save your changes or remove selection from Source and Target assets`;
        }
        else {
            this.topWarningMessage = '';
            this.editorType = type;
            this.loadSettings(false);
        }
    }

    onAssetSearchSelection(event: any) {
        if (this.sourceAssets.length > 0) {
            this.sourceBtnText = $localize`Add another source asset`;
        }
        else {this.sourceBtnText = $localize`Add source asset`;}

        if (this.targetAssets.length > 0) {
            this.targetBtnText = $localize`Add another target asset`;
        }
        else {this.targetBtnText = $localize`Add target asset`;}

        this.checkSelectionValues();
        this.buildTransformationFilters();
        this.buildSourceFilters();
        this.buildTargetFilters();

        this.validateRelationships();

    }

    newAssetAdded($event) {
        const item = new CommonComponentAssetResult();
        item.Uid = $event.assetUid;
        item.AssetTypeUid = $event.assetTypeUid;

        const arr = [];
        arr.push(item);

        this.transformationAsset = arr;
        this.isAddTransformationVisible = false;
        this.onAssetSearchSelection(null);
    }

    onCancel() {
        this.isAddTransformationVisible = false;
    }

    get IsValid(): boolean {

        if (!this.areRelationshipsValid)
            {return false;}

        if (this.isSaving || this.isSavingAndContinue)
            {return false;}

        if (this.editorType == RelationshipEditorType.Lineage && this.sourceAssets.length > 0 && this.transformationAsset.length > 0 && this.targetAssets.length > 0) {
            return true;
        }
        return false;
    }

    resolveAssets() {
        if (this.sourceAssets.length > 0 && this.transformationAsset.length > 0) {
            const transformAsset = this.transformationAsset[0];
        }
    }

    saveAndContinue() {
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

    save() {
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

    private checkPredicateTimeout = null;
    private doMissingPredicateCheck() {
        this.missingPredicateSource = false;
        this.missingPredicateTarget = false;

        this.sourceAssets.forEach((x) => {
            x.Warnings = [];
            if (!x.Predicate) {
                this.missingPredicateSource = true;
            }
        });
        this.targetAssets.forEach((x) => {
            x.Warnings = [];

            if (!x.Predicate) {
                this.missingPredicateTarget = true;
            }
        });

        if (this.sourceAssets.length > 0 && this.transformationAsset.length > 0 && this.targetAssets.length > 0) {
            if (!this.missingPredicateSource && !this.missingPredicateTarget) {
                this.areAllItemsSelected = true;
            }
        }
        this.ref.markForCheck();
    }

    private validateRelationships() {
        this.areRelationshipsValid = false;
        this.relationshipsError = [];

        clearTimeout(this.checkPredicateTimeout);
        this.checkPredicateTimeout = setTimeout(() => this.doMissingPredicateCheck(), 1500);

        const relationships = this.buildRelationshipsFromSelection();

        const resolveRelationshipTasks = [];
        relationships.forEach((r) => {
            resolveRelationshipTasks.push(this.relationshipService.getRelationshipsByAssetTypeUid(r.SubjectAssetTypeUid));
        });

        const resolveRelationshipsObservable = forkJoin(resolveRelationshipTasks);
        resolveRelationshipsObservable.subscribe((results) => {
            const eligibleRelationships = [];
            results.forEach((res) => {
                (<any[]>res).forEach((r) => {
                    if (r.Predicate.Type == 'Transformation') {
                        eligibleRelationships.push(r);
                    }
                });
            });

            relationships.forEach((rel) => {
                const intersectType = eligibleRelationships.find((x) => x.Predicate.Uid == rel.PredicateUid && x.Object.Uid == rel.ObjectAssetTypeUid && x.Subject.Uid == rel.SubjectAssetTypeUid);
                rel.IntersectTypeUid = intersectType ? intersectType.Uid : null;
            });


            const invalidRelationships = relationships.filter((x) => x.IntersectTypeUid == null);
            invalidRelationships.forEach((inv) => {
                inv.Intersects.forEach((rel) => {
                    this.sourceAssets.forEach((sa) => {
                        if (sa.Predicate && inv.PredicateUid == sa.Predicate.Uid.toString() && sa.Uid == rel.SubjectAssetUid) {
                            sa.Warnings = [];
                            sa.Warnings.push($localize`Cannot create relationship of this type!`);
                        }
                    });
                    this.targetAssets.forEach((ta) => {

                        if (ta.Predicate && inv.PredicateUid == ta.Predicate.Uid.toString() && ta.Uid == rel.ObjectAssetUid) {
                            ta.Warnings = [];
                            ta.Warnings.push($localize`Cannot create relationship of this type!`);
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
        const relationships = this.buildRelationshipsFromSelection();

        const resolveRelationshipTasks = [];
        relationships.forEach((r) => {
            resolveRelationshipTasks.push(this.relationshipService.getRelationshipsByAssetTypeUid(r.SubjectAssetTypeUid));
        });

        const resolveRelationshipsObservable = forkJoin(resolveRelationshipTasks);
        resolveRelationshipsObservable.subscribe((results) => {
            const eligibleRelationships = [];
            results.forEach((res) => {
                (<any[]>res).forEach((r) => {
                    if (r.Predicate.Type == 'Transformation') {
                        eligibleRelationships.push(r);
                    }
                });
            });

            relationships.forEach((rel) => {
                const intersectType = eligibleRelationships.find((x) => x.Predicate.Uid == rel.PredicateUid && x.Object.Uid == rel.ObjectAssetTypeUid && x.Subject.Uid == rel.SubjectAssetTypeUid);
                rel.IntersectTypeUid = intersectType ? intersectType.Uid : null;
            });

            if (!relationships.some((x) => x.IntersectTypeUid == null)) {
                this.postRelationships(relationships);
            }
            else {
                this.afterSaveEvent(false);
                relationships.filter((x) => x.IntersectTypeUid == null).forEach((fail) => {
                    const errorMsg = $localize`This lineage relationship cannot be created, as there is no relationship type defined between 2 asset types:`;

                    let subjectTitle = 'Source Asset:';
                    let objectTitle = 'Transformation:';
                    if (fail.type == 'T->S') {
                        subjectTitle = objectTitle;
                        objectTitle = 'Target Asset:';
                    }
                    const subject = this.getAssetFromSelection(fail.Intersects[0].SubjectAssetUid);
                    const object = this.getAssetFromSelection(fail.Intersects[0].ObjectAssetUid);
                    this.relationshipsError.push({ errorMsg, subject, subjectTitle, object, objectTitle });
                });
            }
        });
    }

    private postRelationships(relationships: any[]) {

        const source_tasks = [];
        const target_tasks = [];
        relationships.forEach((r) => {
            if (r.Intersects.some((x) => x.type == 'S->T')) {
                source_tasks.push(this.relationshipService.saveRelationshipsForked(r.IntersectTypeUid, r.Intersects));
            }
            else {
                target_tasks.push(this.relationshipService.saveRelationshipsForked(r.IntersectTypeUid, r.Intersects));
            }

        });

        //Split relationships, and save target after source, so we can properly check for circular relationships
        const sourceObs = forkJoin(source_tasks);
        const targetObs = forkJoin(target_tasks);
        sourceObs.subscribe((results) => {
            this.relationshipsError = [];
            const isSuccess = this.processResults(results);
            if (isSuccess) {
                targetObs.subscribe((res) => {
                    const isSuccess = this.processResults(res);
                    this.afterSaveEvent(isSuccess);
                });
            }
            else {
                this.afterSaveEvent(isSuccess);
            }
        });
    }

    private processResults(results: any[]): boolean {
        let rollback: boolean = false;
        results.forEach((res) => {
            const data = res.obj;
            const result: any[] = res.response;
            result.forEach((r, idx) => {
                if (r.Success == false) {

                    const errorMsg = r.Message;
                    rollback = true;

                    let subjectTitle = 'Source Asset:';
                    let objectTitle = 'Transformation:';
                    if (data.model[idx].type == 'T->S') {
                        subjectTitle = objectTitle;
                        objectTitle = 'Target Asset:';
                    }
                    const subject = this.getAssetFromSelection(data.model[idx].SubjectAssetUid);
                    const object = this.getAssetFromSelection(data.model[idx].ObjectAssetUid);
                    this.relationshipsError.push({ errorMsg, subject, subjectTitle, object, objectTitle });
                }

            });
        });

        //If error occured, delete only newly created relationships
        if (rollback) {
            const deleteTasks = [];
            results.forEach((res) => {
                const ituid = res.obj.intersectTypeUid;
                const rels: any[] = [];
                const arr = <any[]>res.response;
                arr.forEach((rel) => {
                    if (rel.IsNew == true) {
                        rels.push({ uid: rel.uid });
                    }
                });
                deleteTasks.push(this.relationshipService.deleteRelationshipV2(ituid, rels));
            });

            const insertObs = forkJoin(deleteTasks);
            insertObs.subscribe((results) => {
                console.log(results);
            });
            this.ref.markForCheck();

            return false;
        }
        this.ref.markForCheck();
        return true;

    }

    buildRelationshipsFromSelection(): any[] {
        const relationships = [];
        if (this.editorType == RelationshipEditorType.Lineage) {
            const transformation = this.transformationAsset[0];

            if (this.transformationAsset.length != 0) {
                this.sourceAssets.forEach((a) => {
                    const rel1: any = {};
                    rel1.Intersects = [];
                    rel1.SubjectAssetTypeUid = a.AssetTypeUid;
                    rel1.ObjectAssetTypeUid = transformation.AssetTypeUid;
                    if (a.Predicate)
                        {rel1.PredicateUid = a.Predicate.Uid;}
                    else {rel1.PredicateUid = '';}
                    rel1.Intersects.push({ SubjectAssetUid: a.Uid, ObjectAssetUid: transformation.Uid, type: 'S->T' });
                    relationships.push(rel1);
                });
            }

            this.targetAssets.forEach((a) => {
                const rel2: any = {};
                rel2.Intersects = [];
                rel2.ObjectAssetTypeUid = a.AssetTypeUid;
                if (a.Predicate)
                    {rel2.PredicateUid = a.Predicate.Uid;}
                else {rel2.PredicateUid = '';}
                rel2.SubjectAssetTypeUid = transformation.AssetTypeUid;
                rel2.Intersects.push({ ObjectAssetUid: a.Uid, SubjectAssetUid: transformation.Uid, type: 'T->S' });
                relationships.push(rel2);
            });

        }
        return relationships;
    }

    private getAssetFromSelection(assetUid) {
        let result: CommonComponentAssetResult;
        result = this.sourceAssets.find((x) => x.Uid == assetUid);
        if (result === undefined)
            {result = this.transformationAsset.find((x) => x.Uid == assetUid);}

        if (result === undefined)
            {result = this.targetAssets.find((x) => x.Uid == assetUid);}

        return result;
    }

    lineageChainMouseEnter(event) {
        this.helpTextTop = event.clientY + 16;
        this.ref.markForCheck();
    }
}


