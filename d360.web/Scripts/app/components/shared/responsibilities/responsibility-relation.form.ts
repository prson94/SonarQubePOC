import { Input, Output, Component, EventEmitter, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import {
    Permission,
    ResponsibilityTypeAllocation,
    ResponsibilityTypeAllocationPost,
    ResponsibilityTypeRelation,
    ResponsibilityTypeRelationPermission,
    IResponsibilityTypeService,

    ResponsibilityTypeRelation_FormData,
    ResponsibilityTypeRelationAllocationOption
} from '../../../models/responsibility-type.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { BaseComponent } from '../../shared/base.component';

import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

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
            text-transform: uppercase;
            color: #5c5e60 !important;
            font-size: 1rem;
            font-weight: bold;
        }`
    ],
    providers: [ResponsibilityTypeService, ObjectDetailService],
})

export class ResponsibilityRelationForm extends BaseComponent implements OnInit {
    @Input() relation: ResponsibilityTypeAllocation;
    @Input() commonFormData: ResponsibilityTypeRelation_FormData;
    @Output() onComplete = new EventEmitter();
    @Output() onFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private relationPermissions: ResponsibilityTypeRelationPermission[] = [];

    private actionName: string = "Add";
    private inEditModel: boolean = false;
    private selectedAllocation: ResponsibilityTypeRelationAllocationOption = null;
    private errorMessage: string = "";

    public permissionCategories: string[] = ["R", "A", "E", "D"];

    constructor(
        private responsibilityTypeService: ResponsibilityTypeService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private objectDetailService: ObjectDetailService) {
        super(settingsService);
    }

    ngOnInit() {
        this.load();
    }

    private load(): void {

        if (this.relation) {
            if (this.relation.ResponsibilityTypeName?.length > 0) {
                this.inEditModel = true;
                this.actionName = 'Edit';

                //#region Mark the one in use as not used so it will show up in the edit list.
                let ix: ResponsibilityTypeRelationAllocationOption = this.commonFormData.AllocationOptions.find((ao) => ao.Uid === this.relation.AssetTypeUid);
                if (ix) {
                    ix.IsUsed = false;
                    this.selectedAllocation = ix;
                }
                //#endregion

            } else {
                this.inEditModel = false;
                this.actionName = 'Add';
                this.relation.Permissions = this.commonFormData.PermissionOptions.map((p) => { p.Selected = true; return p; });
            }
        }
    }

    //#region form actions

    private cancel(): void {
        this.onCancel.emit(null);
    }

    private updateAssetType(at: ResponsibilityTypeRelationAllocationOption) {
        this.relation.AssetTypeUid = at.Uid;
    }

    private onSubmit(): any {
        this.isLoading = true;

        if (this.validate()) {
            let allocation = new ResponsibilityTypeAllocationPost();
            allocation.AssetTypeUid = this.relation.AssetTypeUid;
            allocation.Permissions = this.relation.Permissions.filter((p) => p.Selected).map((p) => parseInt(p.ID));

            if (this.inEditModel) {
                this.responsibilityTypeService.putResponsibilityTypeAllocations(this.relation.ResponsibilityTypeUid, [allocation])
                    .subscribe(r => {
                        this.isLoading = false;
                        this.showMessageForResult(this.messagesService, r);
                        if (r.type != 'error') {
                            this.onComplete.emit({ action: 'edit', field: this.relation });
                        }
                    });
            } else {
                this.responsibilityTypeService.postResponsibilityTypeAllocations(this.relation.ResponsibilityTypeUid, [allocation])
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

        if (!this.relation.AssetTypeUid) {
            valid = false;
            this.errorMessage += $localize`You must select a valid asset type.`;
        }

        return valid;
    }

    //#endregion
}