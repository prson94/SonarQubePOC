///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { ObjectDetailService } from '../../services/object-detail.service';

@Component({
    selector: 'object-detail',
    templateUrl: 'scripts/app/components/tiles/object-detail.tile.html',
    providers: [ObjectDetailService]
})

export class ObjectDetailTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    private isLoading = false;

    rows = new Array<DetailRow>();
    columns: number;

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
        }

        this.load();
    }

    private load(): void {


        if (this.objectType && this.objectID) {
            this.isLoading = true;
            this.objectDetailService.getObjectDetail(this.objectID, this.objectType)
                .then(data => {
                    this.rows = data.rows;
                    this.columns = data.columns;
                    this.isLoading = false;
                });
        }
    }
}
