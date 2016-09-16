///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { LoadDetail } from '../../models/load.model';
import { LoadService } from '../../services/load.service';
import { GridColumn } from '../../models/grid-definition.model';


@Component({
    selector: 'd3s-load-item-tile',
    templateUrl: 'scripts/app/components/tiles/load-item.tile.html',
    providers: [LoadService]
})

export class LoadItemTile implements OnChanges {
    @Input() id: number;
    @Input() title: string = "Load Details";

    private isLoading = false;

    columns: GridColumn[];
    items: any[];


    constructor(private loadService: LoadService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();
                //this.objectType = changes['objectType'].currentValue;
            }
        }

        this.load();
    }

    load(): void {
        if (this.id == null)
            return;

        this.isLoading = true;

        this.loadService.getLoadColumns(this.id)
            .then(data => {
                this.columns = data;
            })
            .then(() => this.loadService.getLoadItems(this.id))
            .then(data => {
                this.items = data;
                this.isLoading = false;
            });
    }
}
