import { Input, Component, ViewEncapsulation, ChangeDetectionStrategy, Output, EventEmitter, OnChanges, SimpleChange, ChangeDetectorRef } from '@angular/core';
import { forkJoin, Subscription } from 'rxjs';
import { Predicate, PredicateFriendlyType } from '../../../models/predicate.model';
import { RelationshipCount, RelationshipType, RelationshipV2 } from '../../../models/relationship.model';
import { AssetService } from '../../../services/asset.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';

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
export class AddRelationshipComponent extends BaseComponent implements OnChanges {
    @Input() assetUid: string = "";
    @Input() assetTypeUid: string = "";
    @Input() isVisible: boolean = false;

    @Output() onClose = new EventEmitter();

    currentStep: AddRelationshipStep = AddRelationshipStep.Initial;

    loadTypesSub: Subscription;
    loadRelationshipsSub: Subscription;

    relationshipTypes: RelationshipType[] = [];
    relationshipCounts: RelationshipCount[] = [];
    relationships: any[] = [];
    relationshipTypesResolvedNames: any[] = [];

    selectedRelationshipType: any = {};
    selectedAssets: any[] = [];
    assetDetail: any = {};

    savingInProgress: boolean = false;

    simpleSearchTooltipHTML: string = StringConstants.simpleSearchTooltipHTML;

    constructor(private cdRef: ChangeDetectorRef,
        private relationshipService: RelationshipsService,
        private assetService: AssetService,
        private companySettingsService: CompanySettingsService,
        private messagesService: MessagesObservableService) {
        super(companySettingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if ((p === 'assetUid' || p === 'assetTypeUid') && this.assetUid && this.assetTypeUid) {
                this.initialLoad();
            }
        }
    }

    close() {
        this.isVisible = false;
        this.selectedRelationshipType = null;
        this.onClose.emit();
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
                this.relationshipTypes = data[0];
                this.relationshipCounts = data[1];
                this.assetDetail = data[2];
                this.relationshipTypesResolvedNames = [];
                let count: number = 0;

                this.relationshipTypes.forEach((type) => {
                    var rc = this.relationshipCounts.filter((item) => type.Uid.toLocaleLowerCase() === item.IntersectTypeUid.toLocaleLowerCase());
                    let disabledClass = "";

                    if (rc.length > 0) {
                        count = rc[0].Count;
                    }
                    let name: string = "";
                    if (type["IsSubject"]) {
                        name = type.Predicate.Name + " " + type.Object.Name;
                    }
                    else {
                        name = type.Predicate.Inverse + " " + type.Subject.Name;
                    }


                    if (type.Predicate.Type === "InterTypeHierarchy"
                        || type.Predicate.Type === "IntraTypeHierarchy"
                    ) {
                        disabledClass = 'disabled-predicate';
                    }

                    this.relationshipTypesResolvedNames.push({ uid: type.Uid, name: name, count: count, isSelected: false, disabledClass: disabledClass });
                });
                this.relationshipTypesResolvedNames.sort((a, b) => a["name"].localeCompare(b["name"]));
                this.cdRef.detectChanges();
                this.currentStep = AddRelationshipStep.SetRelationshipType;
            });
    }

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

    get isSelectedRelationshipSubject(): boolean {
        return this.selectedType.Subject.Uid.toLowerCase() === this.assetTypeUid.toLowerCase();
    }

    confirmAssets() {
        this.currentStep = AddRelationshipStep.SetCustomFields;
    }

    saveRelationships() {
        this.savingInProgress = true;
        let values: any = {};
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
            for (var p in values) {
                if (p.toUpperCase() === "OBJECTUID") {
                    relationship.ObjectAssetUid = values[p];
                }
                else if (p.toUpperCase() === "SUBJECTUID") {
                    relationship.SubjectAssetUid = values[p];
                }
                else {
                    relationship.Fields[p] = values[p];
                }
            }

            relationships.push(relationship);
        })

        console.log(relationships);
        this.relationshipService.saveRelationships(this.selectedType.Uid, relationships)
            .subscribe((result) => {
                var res = result[0];
                if (res.Success) {
                    let msg = 'Successfully updated';
                    this.showMessageForApiResult(this.messagesService, res, msg);
                    this.savingInProgress = false;
                }
                else {
                    this.savingInProgress = false;

                    this.cdRef.markForCheck();
                    this.showMessageForApiResult(this.messagesService, res);
                }
            });
    }
}
