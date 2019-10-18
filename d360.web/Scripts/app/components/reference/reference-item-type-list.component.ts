import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ReferenceService } from '../../services/reference.service';
import { PermissionsService } from '../../services/permissions.service';
import { ReferenceItemType } from '../../models/reference.model';
import { FormMode } from '../../models/form.model';
import { AssetTypeService } from '../../services/asset-type.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { AssetTypeClass } from '../../models/asset.model';

@Component({
    selector: 'd3s-reference-item-type-list',
    templateUrl: './reference-item-type-list.component.html',
    providers: [ReferenceService, PermissionsService,AssetTypeService],
})

export class ReferenceItemTypeGridComponent extends BaseComponent implements OnInit {
    @Input() selected: ReferenceItemType;
    @Output() selectedChange = new EventEmitter();

    @Input() initialSelectedListId: number;

    private referenceTypes: ReferenceItemType[];
    private _showEditor: boolean = false;
    private _showDelete: boolean = false;
    assetTypeClass: AssetTypeClass = AssetTypeClass.Reference;

    @Output() formModeChange = new EventEmitter<FormMode>();

    private get showEditor(): boolean {
        return this._showEditor;
    }

    private set showEditor(value: boolean) {
        if (value != this._showEditor && value) {
            this.formModeChange.emit(FormMode.Editing | FormMode.Adding);
        }
            
        this._showEditor = value;

        if (!this._showDelete && !this._showEditor) {
            this.formModeChange.emit(FormMode.Default);
        }
    }

    private get showDelete(): boolean {
        return this._showDelete;
    }


    private set showDelete(value: boolean) {
        if (value != this._showDelete && value) {
            this.formModeChange.emit(FormMode.Deleting);
        }

        this._showDelete = value;

        if (!this._showDelete && !this._showEditor) {
            this.formModeChange.emit(FormMode.Default);
        }
    }

    theDeleteCallback: Function;
    
    constructor(
        private referenceService: ReferenceService,
        private permissionsService: PermissionsService,
        private assetTypeService: AssetTypeService,
        private messagesService: MessagesObservableService) {
        super();
        this.showDelete = false;
        this.showEditor = false;
        this.theDeleteCallback = this.deleteReferenceItemType.bind(this);
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);
        this.referenceService.getReferenceItemTypes()
            .subscribe(result => {
                this.referenceTypes = result.sort((a, b) => a.Name.localeCompare(b.Name));
                if (this.referenceTypes.length > 0) {
                    if (this.initialSelectedListId > 0) {                        
                        let index = this.referenceTypes.findIndex(x => x.ID == this.initialSelectedListId);
                        this.initialSelectedListId = 0;
                        if (index >= 0 && index < this.referenceTypes.length) {
                            this.selected = this.referenceTypes[index];
                        }
                        else {
                            this.selected = this.referenceTypes[0];
                        }
                    }
                    else {
                        this.selected = this.referenceTypes[0];
                    }
                    this.selectedChange.emit(this.selected);
                }
                this.isLoading = false;
            });
    }

    private deleteReferenceItemType(id: number) {
        this.isLoading = true;
        this
            .assetTypeService
            .deleteAssetType(id)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);

                if (result.type != 'error') {
                    let index = this.referenceTypes.findIndex(x => x.AssetTypeID == id);
                    if (index >= 0 && index < this.referenceTypes.length) {
                        this.referenceTypes.splice(index, 1);
                    }

                    if (this.referenceTypes.length > 0) {
                        this.selected = this.referenceTypes[0];
                        this.selectedChange.emit(this.selected);
                    }
                }

                this.isLoading = false;
                this.showDelete = false;
            });
    }

    private saveReferenceItemType(event) {                
        this.showEditor = false;

        if (event.id) {
            this.initialSelectedListId = (0 + event.id);
        }

        this.load();
    }
}
