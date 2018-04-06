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

@Component({
    selector: 'd3s-responsibility-item-form',
    templateUrl: './responsibility-item.form.html',
    providers: [ResponsibilityService],
})

export class ResponsibilityItemForm extends BaseComponent implements OnInit {
    @Input() item: ResponsibilityItemDetail;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private model: ResponsibilityEditorModel;

    private message: FormMessage = new FormMessage();
    private itemToSave = new ResponsibilityItem();
    private checkD: ResponsibilityItemDetail[] = [];
    private showVisible: boolean = false;

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
                //if (!this.item.ID) {
                //    this.item.ObjectID = data.responsibility.ObjectID;
                //    this.item.ObjectType = data.responsibility.ObjectType;
                //}
                this.onLoadComplete.emit({ item: this.item });
                this.isLoading = false;
            });
    }

    private getCurrentContext(): void {
        this.isLoading = true;
        let rType = this.model.selectedResponsibilityType;
        let rID = this.model.selectedResource.split("|", 2);
        this.responsibilityService.getResponsibilityDetail(this.model.responsibility.AssetID)
            .then(data => {
                for (var x = 0; x < data.length; x++) {
                    //console.log('this is it: ' + data[x].ResponsibilityTypeID + ' ' + data[x].ResourceID);
                    //console.log('this should be: ' + rType + ' ' + rID[1]);
                    if (data[x].ResponsibilityTypeID.toString() == rType && data[x].ResourceID.toString() == rID[1]) {
                        this.model.responsibility.Context = data[x].Context;
                    }
                    else this.model.responsibility.Context = "";
                }
                this.isLoading = false;
            });
    }

    private save(): void {
        try {
            this.itemToSave.SecurityAsset = this.model.selectedResource.split('|')[0];
            this.itemToSave.SecurityAssetID = parseInt(this.model.selectedResource.split('|')[1]);
            this.itemToSave.ResponsibilityTypeID = parseInt(this.model.selectedResponsibilityType);
            this.itemToSave.Context = this.model.responsibility.Context;

        } catch (exception) {
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
                for (var i = 0; i < this.checkD.length; i++) {
                    //console.log('this is it: ' + this.checkD[i].SecurityAssetID + ' ' + this.checkD[i].ResponsibilityTypeID)
                    //console.log('this should be: ' + this.itemToSave.SecurityAssetID + ' ' + this.itemToSave.ResponsibilityTypeID)
                    if (this.checkD[i].SecurityAssetID == this.itemToSave.SecurityAssetID && this.checkD[i].ResponsibilityTypeID == this.itemToSave.ResponsibilityTypeID) {
                        this.itemToSave.ID = this.checkD[i].OverrideItemID;
                        this.responsibilityService.putResponsibility(this.itemToSave)
                            .then(data => {
                                this.isLoading = false;
                                this.showMessageForResult(this.messagesService, data);
                                this.onSaveComplete.emit({ item: this.itemToSave, message: this.message, initialItem: this.item });
                            });
                        return;
                    }
                }
                this.responsibilityService.postResponsibility(this.itemToSave)
                    .then(data => {
                        this.isLoading = false;
                        this.showMessageForResult(this.messagesService, data);
                        this.onSaveComplete.emit({ item: this.itemToSave, message: this.message, initialItem: this.item });
                    });
            })
    }

    private cancel(): void {
        this.onCancel.emit({ item: this.item });
    }
}
