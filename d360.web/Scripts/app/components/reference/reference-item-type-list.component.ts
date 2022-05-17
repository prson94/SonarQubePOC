import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ReferenceService } from '../../services/reference.service';
import { PermissionsService } from '../../services/permissions.service';
import { ReferenceItemType } from '../../models/reference.model';
import { FormMode } from '../../models/form.model';
import { AssetTypeService } from '../../services/asset-type.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { AssetTypeClass } from '../../models/asset.model';
import { CompanySettingsService } from '../../services/settings.service';
import { Table } from 'primeng/table';
import { NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';

@Component({
    selector: 'd3s-reference-item-type-list',
    templateUrl: './reference-item-type-list.component.html',
    providers: [ReferenceService, PermissionsService, AssetTypeService],
})

export class ReferenceItemTypeGridComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() selected: ReferenceItemType;
    @Output() selectedChange = new EventEmitter();

    @Input() initialSelectedListUid: string;
    public rowsPerPage: number;
    public title: string = $localize`Reference Lists`;
    deleteTitle: string = $localize`Are you sure you want to delete the selected item?`;

    private destroy = new Subject<void>();
    private referenceTypes: ReferenceItemType[];
    private _showEditor: boolean = false;
    private _showDelete: boolean = false;
    assetTypeClass: AssetTypeClass = AssetTypeClass.Reference;

    @Output() formModeChange = new EventEmitter<FormMode>();
    @ViewChild('dt', { static: false }) table: Table;

    get showEditor(): boolean {
        return this._showEditor;
    }

    get assetTypEditorTitle(): string {
        return this.selected != null ? $localize`Edit Reference List` : $localize`Add Reference List`;
    }

    set showEditor(value: boolean) {
        if (value != this._showEditor && value) {
            this.formModeChange.emit(FormMode.Editing | FormMode.Adding);
        }

        this._showEditor = value;

        if (!this._showDelete && !this._showEditor) {
            this.formModeChange.emit(FormMode.Default);
        }
    }

    get showDelete(): boolean {
        return this._showDelete;
    }


    set showDelete(value: boolean) {
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
        public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
        private referenceService: ReferenceService,
        private permissionsService: PermissionsService,
        private assetTypeService: AssetTypeService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
        this.showDelete = false;
        this.showEditor = false;
        this.theDeleteCallback = this.deleteReferenceItemType.bind(this);
    }

    ngOnInit() {
        this.load();
        this.setRowsPerPage();
        this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage, this.title);
    }

    setRowsPerPage(): void {
        this.numberOfRowsByCategoryService.rowsPerPage.pipe(
            takeUntil(this.destroy)
        ).subscribe((rowsPerPage) => {
            this.rowsPerPage = rowsPerPage[this.title];
        });
    }

    private load() {
        this.isLoading = true;
        this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);
        this.assetTypeService.getAssetTypesByClass(AssetTypeClass.Reference)
            .subscribe((data) => {
                var result = data.map((x) => (x as any) as ReferenceItemType);
                this.referenceTypes = result.sort((a, b) => a.Name.localeCompare(b.Name));
                if (this.referenceTypes.length > 0) {
                    if (this.initialSelectedListUid.length > 0) {
                        let index = this.referenceTypes.findIndex(x => x.uid == this.initialSelectedListUid);
                        this.initialSelectedListUid = '';
                        if (index >= 0 && index < this.referenceTypes.length) {
                            this.selected = this.referenceTypes[index];

                            var page = Math.floor(index / 10);
                            if (this.table) {
                                this.table.first = page * 10;
                            }
                        }
                        else {
                            this.selected = this.referenceTypes[0];
                        }
                    }
                    else {
                        this.selected = this.referenceTypes[0];
                    }
                    this.onSelect();
                }
                this.isLoading = false;
            });
    }

    private deleteReferenceItemType(id: number) {
        this.isLoading = true;
        var uid = this.referenceTypes.filter(x => x.AssetTypeID == id)[0].uid;
        this
            .assetTypeService
            .deleteSingleAssetType(uid)
            .subscribe((result) => {
                if (result) {
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
                }
                this.isLoading = false;
                this.showDelete = false;
            });
    }

    private saveReferenceItemType(event) {
        this.showEditor = false;

        if (event.id) {
            this.initialSelectedListUid = (0 + event.id);
        }

        this.load();
    }

    private onSelect() {
        this.assetTypeService.getAssetTypeObjectAndID(this.selected.uid)
            .subscribe((res) => {
                this.selected.ID = +res.ObjectID;
                this.selectedChange.emit(this.selected);
            });
    }

    private onEdit(item: ReferenceItemType) {
        this.selected = item;
        this.assetTypeService.getAssetTypeObjectAndID(this.selected.uid)
            .subscribe((res) => {
                this.selected.ID = +res.ObjectID;
                this.selected.AssetTypeID = +res.Id;
                this.showEditor = true;
            });
    }

    private onDelete(item: ReferenceItemType) {
        this.selected = item;
        this.assetTypeService.getAssetTypeObjectAndID(this.selected.uid)
            .subscribe((res) => {
                this.selected.ID = +res.ObjectID;
                this.selected.AssetTypeID = +res.Id;
                this.showDelete = true;
            });

    }

    ngOnDestroy() {
        this.destroy.next();
        this.destroy.complete();
    }
}
