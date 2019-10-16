import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetTypeClass, AssetTypeEditorModel } from '../../../models/asset.model';
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
    @Input() parentID: number;
    @Input() assetTypeClass: AssetTypeClass;
    @Input() showParentPredicates: boolean = true;
    @Input() showDisplayFormat: boolean = true;
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    action: string = "Edit";    
    model: AssetTypeEditorModel;   
    private isSaving = false;
    AssetTypeClass = AssetTypeClass;

    showAssetStyles: boolean = true;
    showAssetDepthSettings: boolean = false;
    showFusionOwnerSettings: boolean = false;
    showAssetArtifactSettings: boolean = false;
    showNotesField: boolean = false;
    isFusionEnabled: boolean = true;

    private lineageVersion: number = 1;

    constructor(private assetTypeService: AssetTypeService, private messagesService: MessagesObservableService) {
        super();
    }    

    
    ngOnChanges(changes: SimpleChanges): void {
        if (CompanySettings != null && CompanySettings.LineageVersion != null) {
            this.lineageVersion = CompanySettings.LineageVersion;
        }

        for (let p in changes) {
            if (p == 'id' || p == 'parentID' || p == 'topTypeID') {
                this.load();                
            }
        }
    }

    private load(): void {        
        this.isLoading = true;
        
        if (CompanySettings != null && CompanySettings.FusionEnabled != null) {
            this.isFusionEnabled = CompanySettings.FusionEnabled == "false" ? false : true;
        }

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
            case AssetTypeClass.ReferenceItemType:
                this.showAssetStyles = false;
                this.showNotesField = true;
                break;
        }

        this
            .assetTypeService
            .getAssetTypeEditor(
                this.assetTypeClass,
                this.id,
                this.parentID
            )
            .subscribe(data => {
                this.model = data;
                if (this.model.AssetType.Hierarchy.MaximumDepth == 0) {
                    this.model.AssetType.Hierarchy.MaximumDepth = 1;
                }

                if (this.model.Predicates) {
                    this.model.Predicates.unshift({ label: '', value: '' });
                }

                if (this.model.Tokens)
                {
                    this.model.Tokens.unshift({ label: '', value: '' });
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

        if (this.model.AssetType.Uid != null && this.model.AssetType.Uid != '00000000-0000-0000-0000-000000000000')
        {
            this
                .assetTypeService
                .putAssetType(this.model.AssetType)
                .subscribe(data => {
                    this.showMessageForApiResponse(this.messagesService, data);

                    if (data.Success === true) {
                        this.isSaving = false;
                        this.onSuccess.emit(data);
                        this.onComplete.emit(data);
                    } else {
                        this.isSaving = false;
                    }
                });
        }
        else
        {

            delete this.model.AssetType.Uid;

            this.assetTypeService
                .postAssetType(this.model.AssetType)
                .subscribe(data => {
                    this.showMessageForApiResponse(this.messagesService, data);

                    if (data.Success === true) {
                        this.isSaving = false;
                        this.onSuccess.emit(data);
                        this.onComplete.emit(data);
                    } else {
                        this.isSaving = false;
                    }
                });
        }
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
        if (this.showAssetArtifactSettings || this.showAssetDepthSettings || this.showAssetStyles )
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
}