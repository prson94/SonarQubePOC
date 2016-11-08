import { Input, Output, Component, OnChanges, SimpleChange, EventEmitter } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { FormMode } from '../../models/form.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { BaseComponent } from '../shared/base.component';
import { Permission } from '../../models/permission.model'
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-object-definition-tile',
    templateUrl: './object-definition.tile.html',
    providers: [ObjectDetailService],
})

export class ObjectDefinitionTile extends BaseComponent implements OnChanges {
    @Input() objectID: number;
    @Input() objectType: string;

    @Input() hasSynonyms: boolean = true;
    @Input() hasAttributes: boolean = true;

    @Output() onEditComplete = new EventEmitter();
    
    private object: ObjectDetail = null;

    private formMode: FormMode = FormMode.Default;
    FormMode = FormMode;    

    //ideally base permissions would be an input but angular doesnt support this yet
    @Input() objectPermissions: Permission[] = [];

    constructor(private objectDetailService: ObjectDetailService, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {        
        this.load();
    }

    load(): Promise<any> {
        // this is to workaround angular limitaiont with inputs in base classes
        this.permissions = this.objectPermissions;
        if (this.objectType == null || this.objectID == null)
            return Promise.resolve();

        this.isLoading = true;

        let type = (this.objectType.toLowerCase() == 'artifact') ? "1" : this.objectType;

        return this.objectDetailService.getObject(this.objectID, type)
            .then(r => {
                this.object = r;
                this.isLoading = false;
            });
    }

    save(e): void {
        this.load().then(() => {
            this.onEditComplete.emit(this.object);
            //this.headerBreadcrumbService.popLastBreadcrumb();
            //this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.object.Name, null, true, this.objectType, this.object.TypeID));
            this.formMode = FormMode.Default;
        });
    }

    close(): void { 
        this.formMode = FormMode.Default;
    }
}
