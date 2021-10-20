import { Input, Output, Component, OnInit, OnChanges, EventEmitter, NgModule } from '@angular/core';
import { ResponsibilityEditorModel, ResponsibilityItemDetailV2, ResponsibilityItemV2, ResponsibilityOverridePostModel } from '../../../models/responsibility.model';
import { FormMessage, FormHelper } from '../../../models/form.model';
import { SelectItem, SharedModule } from 'primeng/api';
import { ResponsibilityService } from '../../../services/responsibility.service';
import { BaseComponent } from '../../shared/base.component';
import { isNumber } from 'lodash';
import { EditorField } from '../../../models/editor-field.model';
import { StringHelpers } from '../../../static/string-helpers';
import { ResourcesService } from '../../../services/resources.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CoreModule } from '../core.module';
import { TilesModule } from '../tiles/tiles.module';
import { SharedDeleteFormModule } from '../delete.form';
import { SharedFormMessageModule } from '../form-message.part';
import { SharedGridPagingInfoModule } from '../grid-paging-info.component';
import { PipesModule } from '../../../pipes/pipes.module';
import { ResourceMultiSelectGridModule } from '../resource-multiselect-grid.component';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { DropdownModule } from 'primeng/dropdown';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { MultiSelectModule } from 'primeng/multiselect';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

@Component({
    selector: 'd3s-responsibility-item-form',
    templateUrl: './responsibility-item.form.html',
    providers: [ResponsibilityService,ResourcesService],
})

export class ResponsibilityItemForm extends BaseComponent implements OnInit {    
    @Input() item: ResponsibilityItemDetailV2;
    @Input() assetUid: string;
    @Input() assetID: number;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private model: ResponsibilityEditorModel;
    field: EditorField;
    private resourceGrid: boolean = false;
    private message: FormMessage = new FormMessage();  
    private itemToSave = new ResponsibilityItemV2();
    private checkD: ResponsibilityItemDetailV2[] = [];
    private showVisible: boolean = false;
    private resources: SelectItem[] = [];
    private IsResponsibilityDisabled: boolean = false;
    private resouceAssigned: string;

    constructor(private responsibilityService: ResponsibilityService, private messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
       

        this.itemToSave = new ResponsibilityItemV2();
        this.itemToSave.ResponsibilityUid = this.item.ResponsibilityUid;
        this.itemToSave.AssetUid = this.assetUid;
        this.itemToSave.Description = this.item.Description;
        this.itemToSave.ResourceUid = this.item.GroupResourceUid ?? this.item.ResourceUid;

        this.setResouceAssigned();

        if (this.item == null || (!this.item.ResponsibilityUid && !this.assetID)) {
            throw new Error("responsibility-item-editor [item] requires either a ResponsibilityID or a AssetID");
        }
        this.load();
    }

    private load(): void {

        if (this.item == null) {
            this.onLoadComplete.emit({ item: null });
            return;
        }

        this.isLoading = true;

        this.responsibilityService.getResponsibilityItemEditor(this.assetID, null, this.itemToSave.AssetUid, this.itemToSave.ResponsibilityUid, this.itemToSave.ResourceUid)
            .subscribe(data => {
                this.model = data;
                this.onLoadComplete.emit({ item: this.item });
                this.isLoading = false;
            });
    }

    private setResouceAssigned() {
        switch (this.item.ResourceType) {
            case "Group":
            case "Organization":
            case "Organisation":
                this.resouceAssigned = `${this.item.ResourceType}: ${this.item.Group}`;
                break;
            case "User":
                this.resouceAssigned = `${this.item.ResourceType}: ${this.item.Resource}`;
                break;
            default:
                this.resouceAssigned = "";
        }
    }

    private getResponsibilityTypes(): void {
        this.resouceAssigned = "";
        this.showResourceGrid();
    }

    private getAssetType(resource): string{
        let atype;
        switch (resource) {
            case "Group":
            case "G":
                atype = "G";
                break;
            case "User":
            case "Resource":
            case "R":
                atype = "R";
                break;
            case "Organization":
            case "O":
                atype = "O";
                break;
            default:
                atype = null;
                break;
        }
        return atype;
    }

    private save(): void {
        var selectedResourceUid = "";
        try {

            this.itemToSave.ResponsibilityUid = this.model.selectedResponsibilityType;

            var selectedResourceType = "R";
            var selectedResourceID = this.model.selectedResource.split('|')[1];
            
            switch (this.model.selectedResource.split('|')[0]) {
                case "Group":
                case "G":
                    selectedResourceType = "G";
                    break;
                case "Organization":
                case "Organisation":
                case "O":
                    selectedResourceType = "O";
                    break;
            }            

            selectedResourceUid = this.model.resources.find(x => x.Value.split('|')[0] == selectedResourceType && x.Value.split('|')[1] == selectedResourceID).Value.split('|')[2];
            this.itemToSave.ResourceUid = selectedResourceUid;
            this.itemToSave.Description = this.model.responsibility.Context
        }
        catch (exception) {
            this.message.Error("An error occurred while parsing the select item values.");
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.item });
            return;
        }

        this.isLoading = true;

        this.responsibilityService.getResponsibilityDetail(this.itemToSave.AssetUid)
            .subscribe(data => {
                this.checkD = data;

                //Delete existing record before creating new only when description changed
                if (
                    (this.item.ResponsibilityUid && this.item.ResponsibilityUid == this.itemToSave.ResponsibilityUid)
                    &&
                    (this.item.ResourceUid && this.item.ResourceUid == this.itemToSave.ResourceUid)
                ) {
                    if (this.item.Description != this.itemToSave.Description) {
                        this.responsibilityService.deleteResponsibility(this.assetUid, this.item.ResponsibilityUid, this.item.ResourceUid)
                            .subscribe(() => {
                                this.postRequest();
                            });
                    } else {
                        this.cancel();
                    }
                } else {

                    for (var i = 0; i < this.checkD.length; i++) {
                        if (this.checkD[i].ResourceUid == this.itemToSave.ResourceUid &&
                            this.checkD[i].ResponsibilityUid == this.itemToSave.ResponsibilityUid) {

                            this.message.Error("There is already a responsibility assigned for this role and " + this.checkD[i].ResourceType + ".");
                            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.item });
                            return;
                        }
                                             
                    }

                    if ((this.item.ResponsibilityUid && this.item.ResponsibilityUid != this.itemToSave.ResponsibilityUid)
                        ||
                        (this.item.ResourceUid && this.item.ResourceUid != this.itemToSave.ResourceUid)) {
                        this.responsibilityService.deleteResponsibility(this.assetUid, this.item.ResponsibilityUid, this.item.ResourceUid).subscribe(() => { this.postRequest(); });
                    } else {
                        this.postRequest();
                    }  
                }
            });
    }

    private postRequest() {
        var responsibilityOverridePostModel: ResponsibilityOverridePostModel = new ResponsibilityOverridePostModel();
        responsibilityOverridePostModel.ResourceUid = [this.itemToSave.ResourceUid];
        responsibilityOverridePostModel.Description = this.itemToSave.Description;

        this.responsibilityService.postResponsibility(this.itemToSave.AssetUid, this.itemToSave.ResponsibilityUid, responsibilityOverridePostModel)
            .subscribe(data => {                
                this.isLoading = false;
                data.message = this.item.ResponsibilityUid ? "Responsibility successfully updated" : "Responsibility successfully created";
                this.showMessageForResult(this.messagesService, data);
                this.onSaveComplete.emit({ item: this.itemToSave, message: this.message, initialItem: this.item });
            });
    }

    private cancel(): void {
        this.onCancel.emit({ item: this.item });
    }

    private showResourceGrid() {

        let resTypeId;
        let resTypeUid;
        let securityAsset;
        let securityAssetId;
        let selectedRespType = this.model == null ? "" : this.model.selectedResponsibilityType;
        let selectedResource = this.model == null ? "" : this.model.selectedResource;
        if (StringHelpers.isNullOrEmpty(selectedRespType)) {
            resTypeId = 0
        }
        else {
            if (isNumber(this.model.selectedResponsibilityType))
                resTypeId = this.model.selectedResponsibilityType;
            else {
                resTypeUid = this.model.selectedResponsibilityType;
                resTypeId = 0;
            }
            
        }

        if (StringHelpers.isNullOrEmpty(selectedResource)) {
            securityAsset = "";
            securityAssetId = 0;
        }
        else {
            securityAsset = this.getAssetType(this.model.selectedResource.split('|')[0]);
            securityAssetId = this.model.selectedResource.split('|')[1];
        }

        this.field = new EditorField();
        this.field.TypeaheadUri = `form/Responsibility/Resources?assetID=${this.assetID}&resTypeId=${resTypeId}&secAssetType=${securityAsset}&secAssetTypeid=${securityAssetId}&resTypeUid=${resTypeUid}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = false;
        this.field.Value = [];
        this.resourceGrid = true
    }

    private set fieldValue(value){
        this.field.Value = value;

        if (this.field.Value != null && this.field.Value.length > 0) {
            let x = this.field.Value[0];
            this.resouceAssigned = x.split('|')[2];            
            this.resourceGrid = false;
            this.model.selectedResource = x;
        }
        else {
            this.model.selectedResource = null;
        }

    }

    private isValid(): boolean {
        return !(StringHelpers.isNullOrEmpty(this.resouceAssigned) ||
            StringHelpers.isNullOrEmpty(this.model.selectedResponsibilityType));

    }

}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,

        //d3s
        CoreModule,
        TilesModule,
        SharedDeleteFormModule,
        SharedFormMessageModule,
        SharedGridPagingInfoModule,
        PipesModule,
        ResourceMultiSelectGridModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        InputTextModule,
        EditorModule,
        MultiSelectModule,
        SharedModule,
        TableModule,
        TooltipModule,
    ],
    declarations: [
        ResponsibilityItemForm
    ],
    exports: [
        ResponsibilityItemForm
    ],
    providers: []
})
export class ResponsibilityItemFormModule { }
