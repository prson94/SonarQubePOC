import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { SelectItem, CheckboxModule } from 'primeng/primeng';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import {
    Permission,
    ResponsibilityType,
    ResponsibilityTypeRelation,
    ResponsibilityTypeRelationPermission,
    IResponsibilityTypeService,

    ResponsibilityTypeRelation_FormData,
    ResponsibilityTypeRelationAllocationOption
} from '../../../models/responsibility-type.model';
import { MessagesService } from '../../../services/messages.service';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { BaseComponent } from '../../shared/base.component';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-responsibility-relation-form',
    templateUrl: './responsibility-relation.form.html',
    styles: [
        `
        .display-table tr td {
            padding:3px;
            border-radius: 0;
        }
        .relation-table tr td {
            border-radius: 0;
        }
        
       .display-table-title {
            text-align:center;
            width:100%;
            font-family: "Roboto", Tahoma !important;
            text-transform: uppercase;
            color: #5c5e60 !important;
            font-size: 1rem;
            font-weight: bold;
        }`
    ],
    providers: [ResponsibilityTypeService, ObjectDetailService],
})

export class ResponsibilityRelationForm extends BaseComponent implements OnInit {
    @Input() relation: ResponsibilityTypeRelation;
    @Input() commonFormData: ResponsibilityTypeRelation_FormData;
    @Output() onComplete = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private relationPermissions: ResponsibilityTypeRelationPermission[] = [];

    private actionName: string = "Add";
    private inEditModel: boolean = false;
    private selectedAllocation: ResponsibilityTypeRelationAllocationOption = null;
    private errorMessage: string = "";

    constructor(private responsibilityTypeService: ResponsibilityTypeService, private messagesService: MessagesService, private objectDetailService: ObjectDetailService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load(): void {

        if (this.relation) {
            if (this.relation.ObjectID > 0) {
                this.inEditModel = true;
                this.actionName = 'Edit';

                //#region Mark the one in use as not used so it will show up in the edit list.
                let ix: ResponsibilityTypeRelationAllocationOption = this.commonFormData.AllocationOptions.find(ao => ao.ID === this.relation.AssetTypeID);
                if (ix) {
                    ix.IsUsed = false;
                    this.selectedAllocation = ix;
                }
                //#endregion

                //#region Mark permission options as selected based on collection of permissions on resposibility type relation.
                if (this.relation.Permissions) {

                    this.commonFormData.PermissionOptions.forEach(op => {

                        op.Selected = false;    // Reset to default.

                        var permissionOptionIndex = this.relation.Permissions.findIndex(ep => ep.ID === op.ID);
                        if (permissionOptionIndex > -1) {
                            op.Selected = true;
                        }
                    });
                }
                //#endregion

            } else {
                this.inEditModel = false;
                this.actionName = 'Add';

                this.commonFormData.PermissionOptions.forEach(op => {
                    op.Selected = true;    // Reset to default.
                });
            }
        }
    }

    //#region form actions

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private updateAssetType(at: ResponsibilityTypeRelationAllocationOption) {
        this.relation.AssetTypeID = at.ID;
    }

    private onSubmit(): any {
        this.isLoading = true;

        if (this.validate()) {
            this.relation.Permissions = [];
            this.commonFormData.PermissionOptions.forEach(po => {
                if (po.Selected) {
                    this.relation.Permissions.push(po);
                }
            });

            if (this.relation.ObjectID > 0) {
                this.responsibilityTypeService.putRelation(this.relation)
                    .subscribe(r => {
                        this.isLoading = false;
                        this.showMessageForResult(this.messagesService, r);
                        if (r.type != 'error') {
                            this.onComplete.emit({ action: 'edit', field: this.relation });
                        }
                    });
            } else {
                this.responsibilityTypeService.postRelation(this.relation)
                    .subscribe(r => {
                        this.showMessageForResult(this.messagesService, r);
                        this.isLoading = false;
                        if (r.type != 'error') {
                            this.onComplete.emit({ action: 'add', field: this.relation });
                        }
                    });
            }
        }
    }

    private validate(): boolean {
        let valid = true;
        this.errorMessage = '';

        if (!this.relation.AssetTypeID) {
            valid = false;
            this.errorMessage += "You must select a valid asset type.";
        }

        return valid;
    }

    //#endregion
}