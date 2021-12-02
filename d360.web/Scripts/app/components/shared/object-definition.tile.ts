import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter, ChangeDetectorRef, ViewChild } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { ObjectDetail } from '../../models/object-detail.model';
import { BaseComponent } from '../shared/base.component';
import { NymType } from '../../models/object-detail.model';
import { ResponsibilityTypeRelationPermission } from '../../models/responsibility-type.model';
import { FormMode } from '../../models/form.model';
import { AssetService } from '../../services/asset.service';
import { AssetEditorModel } from '../../models/asset.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { SynonymPermission } from '../../models/artifacts.model';
import { CompanySettingsService } from '../../services/settings.service';
import { D3SModal } from './modal/gov-modal.component';

@Component({
    selector: 'd3s-object-definition-tile',
    templateUrl: './object-definition.tile.html',
    providers: [ObjectDetailService, AssetService],
})

export class ObjectDefinitionTile extends BaseComponent implements OnChanges {
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() useV2Api: boolean = false;
    @Input() synonymPermission: SynonymPermission;

    @Input() nymTypes: NymType[] = [];

    @Output() onEditComplete = new EventEmitter();
    @Output() formModeChange = new EventEmitter();

    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    modalEditorVisible: boolean = false;

    protected object: ObjectDetail = null;

    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];

    @ViewChild('modal', { static: false }) modal: D3SModal;

    constructor(
        private objectDetailService: ObjectDetailService,
        private headerActionsService: HeaderActionsService,
        private assetService: AssetService,
        private messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.load();
    }

    load(): void {
        // this is to workaround angular limitaiont with inputs in base classes
        this.permissions = this.objectPermissions;

        if (this.objectType == null || this.objectID == null) {
            return;
        }

        this.isLoading = true;

        let type = (this.objectType.toLowerCase() == 'artifact') ? "1" : this.objectType;

        this.objectDetailService.getObject(this.objectID, type).subscribe(
            r => {
                this.object = r;

                this.isLoading = false;
            }
        );
    }

    editClick() {
        this.formMode = FormMode.Editing;
        this.formModeChange.emit(this.formMode);
        this.modalEditorVisible = true;
        this.cdRef.markForCheck();
    }

    closeEditor() {
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
        this.modalEditorVisible = false;
        this.cdRef.markForCheck();
    }

    save(e): void {
        this.load();

        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was renamed
        this.onEditComplete.emit(this.object);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }

    saveV2(event): void {

        let values: any = {};
        let asset: AssetEditorModel = new AssetEditorModel();
        let assetTypeUid: string;
        asset.Fields = {};

        //takes the form and convert any array values to , separated string values
        for (var p in event.item) {
            if (event.item.hasOwnProperty(p)) {
                if (Array.isArray(event.item[p])) {
                    values[p] = event.item[p].join();
                } else {
                    values[p] = event.item[p];
                }
            }
        }

        //convert to an asset
        for (var p in values) {
            if (p.toUpperCase() == "PARENTUID") {
                asset.ParentUid = values[p];
            }
            else if (p.toUpperCase() == "UID") {
                asset.Uid = values[p];
            }
            else if (p.toUpperCase() == "ASSETTYPEUID") {
                assetTypeUid = values[p];
            }
            else {
                asset.Fields[p] = values[p];
            }
        }

        if (asset.Uid) this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was edited                                
        this.isLoading = false;

        this.load();

        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was renamed
        this.onEditComplete.emit(this.object);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
        this.modalEditorVisible = false;
    }

    getAssetTypePath() {
        return this.object.TypeName + ' > ' + this.object.TextPath;
    }
}
