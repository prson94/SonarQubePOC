import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { SelectItem } from 'primeng/primeng';
import { BaseComponent } from '../shared/base.component';
import * as _ from 'lodash';
import { AssetTypeService } from "../../services/asset-type.services";
import { AssetTypeClass, AssetTypeEditorModel } from "../../models/asset.model";

@Component({
    selector: 'd3s-asset-type-editor-form',
    templateUrl: './admin-asset-type-editor.form.html',
    providers: [AssetTypeService],
})

export class AdminAssetTypeEditorForm implements OnInit, OnChanges {
    @Input() title: string = "Add Asset Type";
    @Input() id: number;
    @Input() parentID: number;
    @Input() topTypeID: number; //For things like fusion type ID
    @Input() assetTypeClass: string;

    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    theAssetTypeClass: AssetTypeClass;
    action: string = "Edit";
    error: any;
    model: AssetTypeEditorModel;
    private isLoading = false;
    private isSaving = false;

    constructor(private assetTypeService: AssetTypeService) {

    }

    ngOnInit() {        

    }

    ngOnChanges(changes: SimpleChanges): void {
        for (let p in changes) {
            if (p == 'id' || p == 'parentID' || p == 'topTypeID') {
                this.load();
                //this.initialItem = _.cloneDeep(this.model);
            }
        }
    }

    private load(): void {
        this.isLoading = true;

        switch (this.assetTypeClass) {
            case 'FA':
                this.theAssetTypeClass = AssetTypeClass.FusionAttribute;
                break;
            case 'G':
                this.theAssetTypeClass = AssetTypeClass.Glossary;
                break;
            case 'M':
                this.theAssetTypeClass = AssetTypeClass.Model;
                break;
            case 'P':
                this.theAssetTypeClass = AssetTypeClass.Policy;
                break;
        }

        this.assetTypeService.getAssetTypeEditor(this.theAssetTypeClass, this.id, this.parentID)
            .then(data => {
                this.model = data;

                if (this.topTypeID)
                {
                    this.model.TopLevelTypeID = this.topTypeID;
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
        if (this.model.AssetType.ID > 0)
            this.assetTypeService.putAssetType(this.model)
                .then(data => {
                    this.isSaving = false;
                    this.onSuccess.emit(data);
                    this.onComplete.emit(data);
                });
        else
            this.assetTypeService.postAssetType(this.model)
                .then(data => {
                    this.isSaving = false;
                    this.onSuccess.emit(data);
                    this.onComplete.emit(data);
                });
    }
};