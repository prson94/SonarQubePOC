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
import { map } from 'rxjs/operators';

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
        this.itemToSave.ID = this.item.OverrideID;
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
            .subscribe(data => {
                this.model = data;
                this.onLoadComplete.emit({ item: this.item });
                this.isLoading = false;
            });
        //if (StringHelpers.isNullOrEmpty(this.resouceAssigned)) {
        //  //  this.showResourceGrid();
        //}
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
        try {

            this.itemToSave.SecurityAsset = this.getAssetType(this.model.selectedResource.split('|')[0]);
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
            .subscribe(data => {
                this.checkD = data;
                if (this.itemToSave.ID && this.itemToSave.ID > 0) {
                    this.responsibilityService.putResponsibility(this.itemToSave)
                        .subscribe(data => {
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

                            this.itemToSave.ID = this.checkD[i].OverrideID;

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
                        .subscribe(data => {
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
        let securityAsset;
        let securityAssetId;
        let selectedRespType = this.model == null ? "" : this.model.selectedResponsibilityType;
        let selectedResource = this.model == null ? "" : this.model.selectedResource;
        if (StringHelpers.isNullOrEmpty(selectedRespType)) {
            resTypeId = 0
        }
        else {
            resTypeId = this.model.selectedResponsibilityType;
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
        this.field.TypeaheadUri = `form/Responsibility/Resources?assetID=${this.item.AssetID}&resTypeId=${resTypeId}&secAssetType=${securityAsset}&secAssetTypeid=${securityAssetId}`;
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
