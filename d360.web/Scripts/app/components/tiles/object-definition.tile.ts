import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { FormMode } from '../../models/form.model';
import { ObjectDetail } from '../../models/object-detail.model';
import { BaseComponent } from '../shared/base.component';
import { Permission } from '../../models/permission.model'

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
    
    private object: ObjectDetail = null;

    private formMode: FormMode = FormMode.Default;
    FormMode = FormMode;    

    //ideally base permissions would be an input but angular doesnt support this yet
    @Input() objectPermissions: Permission[] = [];

    constructor(private objectDetailService: ObjectDetailService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        console.log(this.hasSynonyms);
        this.load();
    }

    load(): void {
        // this is to workaround angular limitaiont with inputs in base classes
        this.permissions = this.objectPermissions;
        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;

        this.objectDetailService.getObject(this.objectID, this.objectType)
            .then(r => {
                this.object = r;
                this.isLoading = false;
            });
    }

    save(e): void {
        this.formMode = FormMode.Default;
    }
    close(): void { 
        this.formMode = FormMode.Default;
    }
}
