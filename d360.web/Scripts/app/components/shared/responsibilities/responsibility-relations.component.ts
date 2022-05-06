import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { IResponsibilityTypeService, ResponsibilityTypeAllocation, ResponsibilityTypeRelation_FormData, ResponsibilityTypeRelationAllocationOption } from '../../../models/responsibility-type.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-responsibility-relations',
    templateUrl: './responsibility-relations.component.html',
    providers: [ResponsibilityTypeService]
})

export class ResponsibilityRelationsComponent extends BaseComponent implements OnChanges {
    @Input() queryType: string;
    @Input() uid: string;

    @Input() title: string = 'Asset Assignment';

    @Input() showAddButton: boolean = true;
    @Input() showEditButton: boolean = true;
    @Input() showDeleteButton: boolean = true;

    @Output() onEdit = new EventEmitter();
    @Output() onAdd = new EventEmitter();
    @Output() onDelete = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onFieldsChanged = new EventEmitter();

    @Input() isEditing = false;
    @Input() isAdding = false;
    @Input() isDeleting = false;

    private IsResponsibilityTypeView: boolean = false;
    private IsAssetTypeView: boolean = false;

    private rows = new Array<ResponsibilityTypeAllocation>();
    private selectedRow = new ResponsibilityTypeAllocation();
    private commonFormData = new ResponsibilityTypeRelation_FormData();

    private theDeleteCallback: Function;


    constructor(
        private responsibilityTypeService: ResponsibilityTypeService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);

        this.theDeleteCallback = this.deleteResponsibilityTypeRelation.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === "uid") {
                this.uid = changes["uid"].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
        }
        this.load();
    }

    load(): void {
        if (this.uid == null) {
            return;
        }

        // Update component title.
        if (this.queryType === 'A') {
            this.title = 'Responsibility Type Assignment';
        }
        else {
            this.title = 'Asset Assignment';
        }

        this.isLoading = true;

        this.responsibilityTypeService.getRelationFormData().subscribe(formData => {
            this.commonFormData = formData;

            if (this.queryType === 'A') {
                this.responsibilityTypeService.getAllocationsByAssetType(this.uid)
                    .subscribe((data) => {
                        this.rows = data;
                        this.selectedRow = null;

                        //#region Remove the already-populated relations from the list of options.
                        this.rows.forEach(e => {
                            let ix: ResponsibilityTypeRelationAllocationOption = this.commonFormData.AllocationOptions.find((ao) => ao.Uid === e.AssetTypeUid);
                            if (ix) {
                                ix.IsUsed = true;
                            }
                        });
                        //#endregion

                        this.isLoading = false;
                    });
            }
            else {
                this.responsibilityTypeService.getAllocationsByResponsibilityType(this.uid)
                    .subscribe((data) => {
                        this.rows = data;
                        this.selectedRow = null;

                        //#region Remove the already-populated relations from the list of options.
                        this.rows.forEach(e => {
                            let ix: ResponsibilityTypeRelationAllocationOption = this.commonFormData.AllocationOptions.find((ao) => ao.Uid === e.AssetTypeUid);
                            if (ix) {
                                ix.IsUsed = true;
                            }
                        });
                        //#endregion

                        this.isLoading = false;
                    });
            }
        });
    }

    edit(item: ResponsibilityTypeAllocation): void {
        this.selectedRow = item;
        this.isEditing = true;
        this.isDeleting = false;
        this.isAdding = false;
        this.onEdit.emit();
    }

    add(): void {
        this.selectedRow = new ResponsibilityTypeAllocation();
        this.selectedRow.ResponsibilityTypeUid = this.uid;
        this.isEditing = true;
        this.isDeleting = false;
        this.onAdd.emit();
    }

    delete(item: ResponsibilityTypeAllocation): void {
        this.selectedRow = item;
        this.isEditing = false;
        this.isDeleting = true;
        this.isAdding = false;
    }

    editComplete(event) {
        this.isEditing = false;
        this.onCancel.emit();
        this.load();
        this.onFieldsChanged.emit();
    }

    deleteResponsibilityTypeRelation() {
        this.responsibilityTypeService.deleteResponsibilityTypeAllocation(this.selectedRow.ResponsibilityTypeUid, this.selectedRow.AssetTypeUid)
            .subscribe((res) => {
                this.showMessageForResult(this.messagesService, res);
                if (!res.isError) {
                    this.isDeleting = false;
                    this.onDelete.emit();
                    this.load();
                }
            });
    }

    get deletePromptText(): string {
        return $localize`Are you sure you want to delete the responsibility relation between [${this.selectedRow?.AssetTypeName}] and [${this.selectedRow?.ResponsibilityTypeName}]? This will remove all assigned responsibilities between these two types and cannot be undone.`;
    }
}