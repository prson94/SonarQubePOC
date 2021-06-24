import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetTypeService } from '../../../services/asset-type.service';
import { FlowObjectType, AssetTypeClass, AssetTypeEditorModel } from '../../../models/asset.model';
import { ApiResult } from '../../../models/apiresult.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-asset-type-editor',
    templateUrl: './asset-type-editor.component.html',
    providers: [AssetTypeService],
})

export class AssetTypeEditorComponent extends BaseComponent implements OnChanges {
    @Input() title: string = "Add Asset Type";
    @Input() id: number;
    @Input() assetTypeUid: string;
    @Input() useUid: boolean = false;
    @Input() parentID: number;
    @Input() assetTypeClass: AssetTypeClass;
    @Input() showDisplayFormat: boolean = true;
    @Input() fusionId: number;
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    action: string = "Edit";
    model: AssetTypeEditorModel;
    private isSaving = false;
    AssetTypeClass = AssetTypeClass;
    selectedFlowType: FlowObjectType;

    showAssetStyles: boolean = true;
    showAssetDepthSettings: boolean = false;
    showFusionOwnerSettings: boolean = false;
    showAssetArtifactSettings: boolean = false;
    showNotesField: boolean = false;
    showParentPredicates: boolean = true;

    private lineageVersion: number = 1;

    private flowObjectDDL: any[] = [];

    constructor(private assetTypeService: AssetTypeService, private messagesService: MessagesObservableService) {
        super();
        this.flowObjectDDL.push({ value: FlowObjectType.Event, label: 'Event' });
        this.flowObjectDDL.push({ value: FlowObjectType.Activity, label: 'Activity' });
        this.flowObjectDDL.push({ value: FlowObjectType.Gateway, label: 'Gateway' });
    }

    ngOnChanges(changes: SimpleChanges): void {

        if (CompanySettings != null && CompanySettings.LineageVersion != null) {
            this.lineageVersion = CompanySettings.LineageVersion;
        }

        let triggerLoad = false;
        for (let p in changes) {
            if (p == 'id' || p == 'parentID' || p == 'topTypeID') {
                triggerLoad = true;
            }
        }

        if (triggerLoad) {
            this.load();
        }
    }


    private load(): void {
        this.isLoading = true;

        switch (+this.assetTypeClass) {
            case AssetTypeClass.FusionAttribute:
                this.showParentPredicates = false;
                break;
            case AssetTypeClass.BusinessAsset:
                this.showAssetArtifactSettings = true;
                this.showFusionOwnerSettings = true;
                break;
            case AssetTypeClass.TechnicalAsset:
                this.showAssetArtifactSettings = true;
                break;
            case AssetTypeClass.Model:
                this.showAssetDepthSettings = true;
                break;
            case AssetTypeClass.Organization:
                break;
            case AssetTypeClass.Policy:
                this.showAssetDepthSettings = true;
                break;
            case AssetTypeClass.Reference:
                this.showAssetStyles = false;
                this.showNotesField = true;
                break;
            case AssetTypeClass.Rule:
                this.showParentPredicates = false;

        }

        if (this.useUid && this.assetTypeUid) {
            this.assetTypeService.getAssetTypeObjectAndID(this.assetTypeUid).subscribe(res => {
                this.id = res.Id;
                this.loadEditor();
            });
        } else {
            this.loadEditor();
        }
    }

    private loadEditor() {
        this
            .assetTypeService
            .getAssetTypeEditor(this.assetTypeClass, this.id, this.parentID)
            .subscribe(data => {
                this.model = data;
                if (this.model.AssetType.Hierarchy.MaximumDepth == 0) {
                    this.model.AssetType.Hierarchy.MaximumDepth = 1;
                }
                if (this.model.Predicates) {
                    this.model.Predicates.unshift({ label: '', value: '' });
                }
                if (this.model.Tokens) {
                    this.model.Tokens.unshift({ label: '', value: '' });
                }
                if (this.model.AssetType.ParentUid == null || this.model.Predicates == null || this.model.Predicates.length < 2) {
                    this.showParentPredicates = false;
                }
                if (this.assetTypeClass == AssetTypeClass.Reference || this.assetTypeClass == AssetTypeClass.Model || this.assetTypeClass == AssetTypeClass.Policy) {
                    this.showParentPredicates = true;
                }
                if ((this.assetTypeClass == AssetTypeClass.BusinessAsset || this.assetTypeClass == AssetTypeClass.TechnicalAsset)
                    && this.model.AssetType.AutoDisplayParent === null && this.model.AssetType.ParentUid != null) {
                    this.model.AssetType.AutoDisplayParent = true;
                }
                if (this.model.AssetType.CanEditParent === null &&
                    (this.assetTypeClass === AssetTypeClass.BusinessAsset || this.assetTypeClass === AssetTypeClass.TechnicalAsset)
                    && this.model.AssetType.ParentUid !== null
                ) {
                    this.model.AssetType.CanEditParent = true;
                }
                this.isLoading = false;
            });
    }

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private save(): void {
        this.isSaving = true;
        this.model.AssetType.Class = this.assetTypeClass;
        if (this.fusionId)
            this.model.AssetType.FusionID = this.fusionId;

        if (this.model.AssetType.Class != AssetTypeClass.DiagramAsset) {
            delete this.model.AssetType.FlowObjectType;
        }
        else if (!this.model.AssetType.FlowObjectType) {
            this.model.AssetType.FlowObjectType = FlowObjectType.Event;
        }

        if (this.model.AssetType.Uid != null && this.model.AssetType.Uid != '00000000-0000-0000-0000-000000000000') {
            this
                .assetTypeService
                .putAssetType(this.model.AssetType)
                .subscribe(data => {
                    if (data != null && data.Success === true) {
                        this.showMessageForApiResponse(this.messagesService, data);
                        this.isSaving = false;
                        this.onSuccess.emit(data);
                        this.onComplete.emit(data);
                    } else {
                        this.isSaving = false;
                    }
                });
        }
        else {

            delete this.model.AssetType.Uid;

            this.assetTypeService
                .postAssetType(this.model.AssetType)
                .subscribe(data => {
                    if (data != null && data.Success === true) {
                        this.showMessageForApiResponse(this.messagesService, data);
                        this.isSaving = false;
                        this.onSuccess.emit(data);
                        this.onComplete.emit(data);
                    } else {
                        this.isSaving = false;
                    }
                });
        }
    }


    ShowAutoDisplayOption() {
        if (this.showParentPredicates && this.model.AssetType.Hierarchy.PredicateUid) {
            let selectedPredicate = this.model.Predicates.filter(x => x.value.toLowerCase() == this.model.AssetType.Hierarchy.PredicateUid.toLowerCase());
            if (selectedPredicate.length > 0) {
                return true;
            }
        }
        return false;
    }
    private selectToken(e: any) {
        if (e == null)
            return;

        let value = e.value;

        if (e.value == null || e.value == '' || e.value == 'null')
            return;

        if (this.model.AssetType.DisplayFormat == null)
            this.model.AssetType.DisplayFormat = '';
        this.model.AssetType.DisplayFormat += e.value;
    }

    get FirstColumnStyle(): string {
        if (this.showAssetArtifactSettings || this.showAssetDepthSettings || this.showAssetStyles)
            return "col l8 m12 s12";
        return "col s12";
    }

    public updateParent(predicateUid: string) {
        if (predicateUid == null || predicateUid.length == 0) {
            this.model.AssetType.ParentUid = null;
        }
    }

    get isPredicateRequired(): boolean {
        return this.assetTypeClass == AssetTypeClass.Model || this.assetTypeClass == AssetTypeClass.Policy;
    }

    public selectFlowObject($event) {
        console.log($event);
    }

    showCanEditParentOption() {
        if (this.assetTypeClass !== AssetTypeClass.BusinessAsset && this.assetTypeClass !== AssetTypeClass.TechnicalAsset) {
            return false;
        }
        if (this.model.AssetType.ParentUid === null) {
            return false;
        }
        return true;
    }
}
