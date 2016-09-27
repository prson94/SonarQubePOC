
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { ObjectDetailService } from '../../services/object-detail.service';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { FormMode } from '../../models/form.model';
import { ObjectDetail } from '../../models/object-detail.model';


@Component({
    selector: 'd3s-object-definition-tile',
    templateUrl: 'scripts/app/components/tiles/object-definition.tile.html',
    providers: [ObjectDetailService],
})

export class ObjectDefinitionTile implements OnChanges {
    @Input() objectID: number;
    @Input() objectType: string;

    private object: ObjectDetail = null;

    private formMode: FormMode = FormMode.Default;
    FormMode = FormMode;
    private isLoading = false;

    constructor(private objectDetailService: ObjectDetailService) {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {

        this.load();
    }

    load(): void {

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
