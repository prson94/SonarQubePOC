import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { ResponsibilityTypeService } from '../../../services/responsibility-type.service';
import { ResponsibilityType, IResponsibilityTypeService, ResponsibilityTypeRelationRule, ResponsibilityTypeRelation, ResponsibilityTypeRelation_FormData, ResponsibilityTypeRelationAllocationOption } from '../../../models/responsibility-type.model';
import { BaseComponent } from '../../shared/base.component';
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-responsibility-relations',
    templateUrl: './responsibility-relations.component.html',
    providers: [ResponsibilityTypeService ]
})

export class ResponsibilityRelationsComponent extends BaseComponent implements OnChanges {
    @Input() queryType: string;
    @Input() id: number;

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

    private rows = new Array<ResponsibilityTypeRelation>();
    private selectedRow = new ResponsibilityTypeRelation();
    private commonFormData = new ResponsibilityTypeRelation_FormData();

    private theDeleteCallback: Function;

    
    constructor(private responsibilityTypeService: ResponsibilityTypeService, private messagesService: MessagesObservableService) {
        super();

        this.theDeleteCallback = this.deleteResponsibilityTypeRelation.bind(this);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.id = changes['id'].currentValue;
                this.isEditing = false;
                this.isAdding = false;
                this.isDeleting = false;
            }
        }
        this.load();
    }

    load(): void {

        if (this.id == null)
            return;

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
                this.responsibilityTypeService.getRelationsByAssetType(this.id)
                    .subscribe(data => {
                        this.rows = data;
                        this.selectedRow = null;

                        //#region Remove the already-populated relations from the list of options.
                        this.rows.forEach(e => {
                            let ix: ResponsibilityTypeRelationAllocationOption = this.commonFormData.AllocationOptions.find(ao => ao.ID === e.AssetTypeID)
                            if (ix) {
                                ix.IsUsed = true;
                            }
                        });
                        //#endregion

                        this.isLoading = false;
                    });
            }
            else {
                this.responsibilityTypeService.getRelationsByResponsibilityType(this.id)
                    .subscribe(data => {
                        this.rows = data;
                        this.selectedRow = null;

                        //#region Remove the already-populated relations from the list of options.
                        this.rows.forEach(e => {
                            let ix: ResponsibilityTypeRelationAllocationOption = this.commonFormData.AllocationOptions.find(ao => ao.ID === e.AssetTypeID)
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

    edit(item: ResponsibilityTypeRelation): void {
        this.selectedRow = item;
        this.isEditing = true;
        this.isDeleting = false;
        this.isAdding = false;
        this.onEdit.emit();
    }

    add(): void {
        this.selectedRow = new ResponsibilityTypeRelation();
        this.selectedRow.ResponsibilityTypeID = this.id;
        this.isEditing = true;
        this.isDeleting = false;
        this.onAdd.emit();
    }

    delete(item: ResponsibilityTypeRelation): void {
        this.selectedRow = item;
        this.isEditing = false;
        this.isDeleting = true;
        this.isAdding = false;
        //this.onDelete.emit();
    }

    editComplete(event) {
        this.isEditing = false;
        this.onCancel.emit();
        this.load();
        this.onFieldsChanged.emit();
    }

    deleteResponsibilityTypeRelation(id: number) {
        this.responsibilityTypeService.deleteRelation(this.selectedRow).subscribe(res => {
            this.showMessageForResult(this.messagesService, res);
            if (!res.isError){
                this.isDeleting = false;
                //let index = this.rows.findIndex(f => f.ID == id);
                //if (index >= 0 && index < this.rows.length)
                //    this.rows.splice(index, 1);
                this.onDelete.emit();
                this.load();
            }
        });
    }

    private htmlDecode(val: string): string {
        return val ? String(val).replace(/<[^>]+>/gm, '') : '';
    }
}