import { Input, Component, ViewEncapsulation, ChangeDetectionStrategy, Output, EventEmitter, OnChanges, SimpleChange, ChangeDetectorRef, OnInit } from '@angular/core';
import { forkJoin, Subject, Subscription } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { RelationshipCount, RelationshipType, RelationshipV2 } from '../../../models/relationship.model';
import { AssetService } from '../../../services/asset.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';

import { RelationshipsService } from '../../../services/relationships.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { StringConstants } from '../../../static/string-constants';
import { BaseComponent } from '../base.component';

enum AddRelationshipStep {
    Initial = 'Initial',
    SetRelationshipType = 'SetRelationshipType',
    SetAssets = 'SetAssets',
    SetCustomFields = 'SetCustomFields',
    Finish = 'Finish'
}

@Component({
    selector: 'add-relationship-editor',
    templateUrl: './add-relationship.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['add-relationship.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [RelationshipsService]
})
export class AddRelationshipComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() assetUid: string = "";
    @Input() assetTypeUid: string = "";
    @Input() isVisible: boolean = false;
    @Input() isFromModal: boolean = false;

    @Output() onClose = new EventEmitter();
    @Output() onAddComplete = new EventEmitter();

    currentStep: AddRelationshipStep = AddRelationshipStep.Initial;

    loadTypesSub: Subscription;
    loadRelationshipsSub: Subscription;

    relationshipTypes: RelationshipType[] = [];
    relationshipCounts: RelationshipCount[] = [];
    relationships: any[] = [];
    relationshipTypesResolvedNames: any[] = [];

    selectedRelationshipType: any = {};
    selectedAssets: any[] = [];
    selectedAssetsDetail: any[] = [];
    fieldValues: any = {};
    assetDetail: any = {};
    higlightedItem: any = {};

    savingInProgress: boolean = false;
    previewAssetUid: string = "";
    previewAssetType: string = "";

    simpleSearchTooltipHTML: string = StringConstants.simpleSearchTooltipHTML;

    constructor(private cdRef: ChangeDetectorRef,
        public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        private relationshipService: RelationshipsService,
        private assetService: AssetService,
        private companySettingsService: CompanySettingsService,
        private messagesService: MessagesObservableService) {
        super(companySettingsService);
    }

    public rowsPerPage: number;
    public title: string = 'Add Relationships Lists'
    private destroy = new Subject<void>();

    ngOnInit() {
        this.setRowsPerPage();
        this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage);
    }

    setRowsPerPage(): void {
        this.numberOfRowsByCategoryService.rowsPerPage.pipe(
            takeUntil(this.destroy)
        ).subscribe((rowsPerPage) => {
            this.rowsPerPage = rowsPerPage[this.title] || this.defaultInitialItemsPerPage;
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if ((p === 'assetUid' || p === 'assetTypeUid') && this.assetUid && this.assetTypeUid) {
                this.initialLoad();
            }
        }
    }

    ngOnDestroy() {
        this.destroy.next();
        this.destroy.complete();
    }

    close() {
        this.isVisible = false;
        this.currentStep = AddRelationshipStep.SetRelationshipType;
        this.selectedRelationshipType = null;
        this.previewAssetUid = this.previewAssetType = "";
        this.resetSelectedAssets();
        this.onClose.emit();
    }

    resetSelectedAssets() {
        this.selectedAssets = [];
        this.selectedAssetsDetail = [];
    }

    setDisabledClassOnConditions(count: number, thisCardinality: string, targetCardinality: string, type: RelationshipType): string {
        let disabledClass: string = "";

        if (count > 0 && thisCardinality === "One" && targetCardinality === "Many") {
            disabledClass = 'disabled-cardinality-many';
        }
        if (count > 0 && targetCardinality === "One" && thisCardinality === "One") {
            disabledClass = 'disabled-cardinality-one';
        }

        if (type.Predicate.Type === "InterTypeHierarchy"
            || type.Predicate.Type === "IntraTypeHierarchy"
        ) {
            disabledClass = 'disabled-predicate';
        }

        return disabledClass;
    }

    public initialLoad(): void {
        if (this.loadTypesSub) {
            this.loadTypesSub.unsubscribe();
        }

        this.loadTypesSub = forkJoin(
            this.relationshipService.getRelationshipsByAssetTypeUid(this.assetTypeUid),
            this.relationshipService.getRelationshipsCountsForAsset(this.assetUid),
            this.assetService.getUIDetailsForAssetUID(this.assetUid)
        )
            .subscribe((data) => {
                this.relationshipTypes = data[0].filter((type) => type.Predicate.Type !== 'Diagram');
                this.relationshipCounts = data[1];
                this.assetDetail = data[2];
                this.relationshipTypesResolvedNames = [];

                this.relationshipTypes.forEach((type) => {

                    let count: number = 0;
                    let disabledClass: string = "";
                    let name: string = "";
                    let thisCardinality: string = "";
                    let targetCardinality: string = "";
                    let rc = this.relationshipCounts.filter((item) => type.Uid.toLocaleLowerCase() === item.IntersectTypeUid.toLocaleLowerCase());
                    if (rc.length > 0) {
                        count = rc[0].Count;
                    }

                    if (type.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase()) {
                        name = type.Predicate.Name + " " + type.Object.Name;
                        targetCardinality = type.Subject.Cardinality;
                        thisCardinality = type.Object.Cardinality;
                        disabledClass = this.setDisabledClassOnConditions(count, thisCardinality, targetCardinality, type);
                        this.relationshipTypesResolvedNames.push({ uid: type.Uid, name, count, isSelected: false, disabledClass, perspective: "Subject" });
                    }

                    if (type.Object.Uid.toLowerCase() === this.assetTypeUid.toLowerCase()) {
                        name = type.Predicate.Inverse + " " + type.Subject.Name;
                        targetCardinality = type.Object.Cardinality;
                        thisCardinality = type.Subject.Cardinality;
                        disabledClass = this.setDisabledClassOnConditions(count, thisCardinality, targetCardinality, type);
                        this.relationshipTypesResolvedNames.push({ uid: type.Uid, name, count, isSelected: false, disabledClass, perspective: "Object" });
                    }

                });
                this.relationshipTypesResolvedNames.sort((a, b) => a["name"].localeCompare(b["name"]));
                this.cdRef.detectChanges();
                this.currentStep = AddRelationshipStep.SetRelationshipType;
            });
    }

    //get selectedPerspective(): string {
    //    return this.selectedRelationshipType.perspective;
    //}

    get selectedType(): RelationshipType {
        var type = this.relationshipTypes.filter((x) => x.Uid === this.selectedRelationshipType.uid);
        return type[0];
    }

    get selectedRelationshipHasCustomFields(): boolean {
        return this.selectedType.HasFieldTypes;
    }

    get targetTypeUid(): string {
        return this.isSelectedRelationshipSubject ? this.selectedType.Object.Uid : this.selectedType.Subject.Uid;
    }

    get targetTypeCardinality(): number {
        var cardinality = this.isSelectedRelationshipSubject ? this.selectedType.Object.Cardinality : this.selectedType.Subject.Cardinality;
        return cardinality === "One" ? 1 : 2;
    }

    get subjectTypeCardinality(): number {
        var cardinality = this.isSelectedRelationshipSubject ? this.selectedType.Subject.Cardinality : this.selectedType.Object.Cardinality;
        return cardinality === "One" ? 1 : 2;
    }

    get targetType(): string {
        return this.isSelectedRelationshipSubject ? this.selectedType.Object.Class : this.selectedType.Subject.Class;
    }

    get target(): any {
        return this.isSelectedRelationshipSubject ? this.selectedType.Object : this.selectedType.Subject;
    }

    get isSelectedRelationshipSubject(): boolean {
        //return this.selectedType.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase();
        return (this.selectedRelationshipType.perspective == "Subject");
    }

    get modalSubtitle(): string {
        let title: string = this.assetDetail.DisplayValue;
        if (this.currentStep === AddRelationshipStep.SetAssets) {
            title += " - " + `${this.selectedRelationshipType.name}`;
        }
        if (this.currentStep === AddRelationshipStep.SetCustomFields) {
            title += " -" + `&nbsp;<strong>${this.selectedRelationshipType.name}</strong>&nbsp;`;
            if (this.selectedAssetsDetail.length > 1) {
                title += "- " + this.selectedAssetsDetail.length + " items";
            }
            else if (this.selectedAssetsDetail[0]) {

                title += "- " + this.selectedAssetsDetail[0].Text;
            }
            else if (this.selectedAssetsDetail && !Array.isArray(this.selectedAssetsDetail)) {
                title += "- " + this.selectedAssetsDetail["Text"];
            }
        }
        return title;
    }

    get selectedAssetCount(): number {
        if (!this.selectedAssetsDetail) {
            return 0;
        }
        return this.selectedAssetsDetail.length;
    }

    confirmAssets() {
        this.previewAssetUid = '';
        this.currentStep = AddRelationshipStep.SetCustomFields;
    }

    saveRelationships() {
        this.previewAssetUid = '';
        this.savingInProgress = true;
        let relationships: RelationshipV2[] = [];

        this.selectedAssets.forEach((asset) => {
            var relationship = new RelationshipV2();
            if (this.isSelectedRelationshipSubject) {
                relationship.SubjectAssetUid = this.assetUid;
                relationship.ObjectAssetUid = asset;
            }
            else {
                relationship.ObjectAssetUid = this.assetUid;
                relationship.SubjectAssetUid = asset;
            }
            relationship.Fields = {};

            //convert artifact to an asset
            for (var p in this.fieldValues) {
                relationship.Fields[p] = this.fieldValues[p];
            }

            relationships.push(relationship);
        });

        this.relationshipService.saveRelationships(this.selectedType.Uid, relationships)
            .subscribe((result) => {
                var res = result[0];
                if (res.Success) {
                    let msg = 'Successfully updated';
                    this.showMessageForApiResult(this.messagesService, res, msg);
                    this.savingInProgress = false;
                    this.previewAssetUid = this.previewAssetType = "";
                    this.currentStep = AddRelationshipStep.SetRelationshipType;
                    this.onAddComplete.emit(null);
                }
                else {
                    this.savingInProgress = false;

                    this.cdRef.markForCheck();
                    this.showMessageForApiResult(this.messagesService, res);
                }
            });
    }

    updateFields($event) {
        this.fieldValues = $event.values;
    }

    get selectionScrollHeight(): string {
        return (window.innerHeight - 380) + "px";
    }

    onInfoClick($event) {
        if (!$event) {
            this.previewAssetType = this.previewAssetUid = "";
            return;
        }
        this.previewAssetUid = $event.Value;
        if (this.targetType === "BusinessAsset" || this.targetType === "TechnicalAsset") {
            this.previewAssetType = "Artifact";
        }
        else if (this.targetType === "Reference") {
            this.previewAssetType = "ReferenceItem";
        }
        else if (this.targetType === "User") {
            this.previewAssetType = "Resource";
        }
        else if (this.targetType === "Model") {
            this.previewAssetType = "Taxonomy";
        }
        else {
            this.previewAssetType = this.targetType;
        }
    }
}
