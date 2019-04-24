import {Input, Output, Component, OnChanges, SimpleChange, EventEmitter} from '@angular/core';
import {ObjectDetailService} from '../../services/object-detail.service';
import {HeaderActionsService} from '../../services/header-actions.service';
import {DetailRow, DetailField, DetailModel, IObjectDetailService} from '../../models/object-detail.model';
import {ObjectDetail} from '../../models/object-detail.model';
import {BaseComponent} from '../shared/base.component';
import {NymType} from '../../models/object-detail.model';
import {ResponsibilityTypeRelationPermission} from '../../models/responsibility-type.model';
import {FormMode} from '../../models/form.model';

@Component({
    selector: 'd3s-object-definition-tile',
    templateUrl: './object-definition.tile.html',
    providers: [ObjectDetailService],
})

export class ObjectDefinitionTile extends BaseComponent implements OnChanges {
    @Input() objectID: number;
    @Input() objectType: string;

    @Input() hasAttributes: boolean = true;
    @Input() nymTypes: NymType[] = [];

    @Output() onEditComplete = new EventEmitter();
    @Output() formModeChange = new EventEmitter();

    private formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    private object: ObjectDetail = null;

    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];

    constructor(
        private objectDetailService: ObjectDetailService,
        private headerActionsService: HeaderActionsService
    ) {
        super();
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

    save(e): void {
        this.load();

        this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was renamed
        this.onEditComplete.emit(this.object);
        this.formMode = FormMode.Default;
        this.formModeChange.emit(this.formMode);
    }
}
