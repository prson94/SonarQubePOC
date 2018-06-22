import { Input, Output, Component, OnInit, OnChanges, EventEmitter } from '@angular/core';
import { ResponsibilityItem, ResponsibilityItemDetail, ResponsibilityEditorModel } from '../../../models/responsibility.model';
import { FormMessage, FormHelper } from '../../../models/form.model';
import { SelectItem, Editor } from 'primeng/primeng';
import { MessagesService } from '../../../services/messages.service';
import { ResponsibilityService } from '../../../services/responsibility.service';
import { BaseComponent } from '../../shared/base.component';
import * as _ from 'lodash';
import { isDate } from 'util';
import { JsonResult } from '../../../models/jsonresult.model';
import { EditorField } from '../../../models/editor-field.model';
import { StringHelpers } from '../../../static/string-helpers';
import { ResourcesService } from '../../../services/resources.service';

@Component({
    selector: 'd3s-responsibility-item-form',
    templateUrl: './responsibility-item.form.html',
    providers: [ResponsibilityService,ResourcesService],
})

export class ResponsibilityItemForm extends BaseComponent implements OnInit {
    @Input() item: ResponsibilityItemDetail;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private model: ResponsibilityEditorModel;
    field: EditorField;
    private resourceGrid: boolean = false;
    private message: FormMessage = new FormMessage();
    private itemToSave = new ResponsibilityItem();
    private checkD: ResponsibilityItemDetail[] = [];
    private showVisible: boolean = false;
    private resources: SelectItem[] = [];
    private IsResponsibilityDisabled: boolean = false;
    private resouceAssigned: string;

    constructor(private responsibilityService: ResponsibilityService, private messagesService: MessagesService) {
        super();
    }

    ngOnInit() {
        this.itemToSave = new ResponsibilityItem();
        this.itemToSave.AssetID = this.item.AssetID;
        this.itemToSave.ID = this.item.OverrideItemID;
        this.itemToSave.ResponsibilityTypeID = this.item.ResponsibilityTypeID;
        this.itemToSave.SecurityAsset = this.item.SecurityAsset;
        this.itemToSave.SecurityAssetID = this.item.SecurityAssetID;
        this.itemToSave.Context = this.item.Context;
        
        this.setResouceAssigned();

        if (this.item == null || (this.itemToSave.ID < 0 && !this.itemToSave.AssetID)) {
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

        this.responsibilityService.getResponsibilityItemEditor(this.itemToSave.AssetID, this.itemToSave.ID)
            .then(data => {
                this.model = data;
                 this.onLoadComplete.emit({ item: this.item });
                this.isLoading = false;
            });
        if (StringHelpers.isNullOrEmpty(this.resouceAssigned)) {
            this.showResourceGrid();
        }
    }

    private setResouceAssigned() {
        switch (this.item.SecurityAsset) {
            case "G":
                this.resouceAssigned = `Group: ${this.item.SecurityAssetName}`;
                break;
            case "R":
                this.resouceAssigned = `User: ${this.item.SecurityAssetName}`;
                break;
            case "O":
                this.resouceAssigned = `Organization: ${this.item.SecurityAssetName}`;
                break;
            default:
                this.resouceAssigned = "";
            
        }
    }
    private getResponsibilityTypes(): void {

        this.resouceAssigned = "";
        this.showResourceGrid();

 
    }



    private save(): void {
        try {
            debugger;
            switch (this.model.selectedResource.split('|')[0]) {
                case "Group":
                case "G":
                    this.itemToSave.SecurityAsset = "G";
                    break;
                case "User":
                case "Resource":
                case "R":
                    this.itemToSave.SecurityAsset = "R";
                    break;
                case "Organization":
                case "O":
                    this.itemToSave.SecurityAsset = "O";
                    break;
                default:
                    this.itemToSave.SecurityAsset = null;
                    break;
            }

  
            this.itemToSave.SecurityAssetID = parseInt(this.model.selectedResource.split('|')[1]);
            this.itemToSave.ResponsibilityTypeID = parseInt(this.model.selectedResponsibilityType);
            this.itemToSave.Context = this.model.responsibility.Context;
        }
        catch (exception) {
            this.message.Error("An error occurred while parsing the select item values.");
            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.item });
            return;
        }

        this.isLoading = true;

        this.responsibilityService.getResponsibilityDetail(this.itemToSave.AssetID)
            .then(data => {
                this.checkD = data;
            })
            .then(() => {
                if (this.itemToSave.ID && this.itemToSave.ID > 0) {
                    this.responsibilityService.putResponsibility(this.itemToSave)
                        .then(data => {
                            this.isLoading = false;
                            this.showMessageForResult(this.messagesService, data);
                            this.onSaveComplete.emit({ item: this.itemToSave, message: this.message, initialItem: this.item });
                        });
                }
                else {
                    for (var i = 0; i < this.checkD.length; i++) {
                        if (this.checkD[i].SecurityAsset == this.itemToSave.SecurityAsset &&
                            this.checkD[i].SecurityAssetID == this.itemToSave.SecurityAssetID &&
                            this.checkD[i].ResponsibilityTypeID == this.itemToSave.ResponsibilityTypeID) {

                            this.itemToSave.ID = this.checkD[i].OverrideItemID;
                            //this.responsibilityService.putResponsibility(this.itemToSave)
                            //    .then(data => {
                            //        this.isLoading = false;
                            //        this.showMessageForResult(this.messagesService, data);
                            //        this.onSaveComplete.emit({ item: this.itemToSave, message: this.message, initialItem: this.item });
                            //    });
                            var securityAssetDisplayNameForError = "user";
                            switch (this.itemToSave.SecurityAsset) {
                                case "G":
                                    securityAssetDisplayNameForError = "group";
                                    break;
                                case "O":
                                    securityAssetDisplayNameForError = "organization";
                                    break;
                            }
                            this.message.Error("There is already a responsibility assigned for this role and " + securityAssetDisplayNameForError + ".");
                            this.onSaveComplete.emit({ item: this.item, message: this.message, initialItem: this.item });
                            return;
                        }
                    }
                    this.responsibilityService.postResponsibility(this.itemToSave)
                        .then(data => {
                            this.isLoading = false;
                            this.showMessageForResult(this.messagesService, data);
                            this.onSaveComplete.emit({ item: this.itemToSave, message: this.message, initialItem: this.item });
                        });
                }
            });
    }

    private cancel(): void {
        this.onCancel.emit({ item: this.item });
    }

    private showResourceGrid() {
        let resTypeId;
        if (StringHelpers.isNullOrEmpty(this.model.selectedResponsibilityType))
            resTypeId = 0
        else
            resTypeId = this.model.selectedResponsibilityType;
        console.log(resTypeId);
        this.field = new EditorField();
        this.field.TypeaheadUri = `form/Responsibility/Resources?assetID=${this.item.AssetID}&resTypeId=${resTypeId}`;
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
