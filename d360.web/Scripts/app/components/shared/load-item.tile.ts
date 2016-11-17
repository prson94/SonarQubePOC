import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { LoadDetail } from '../../models/load.model';
import { LoadService } from '../../services/load.service';
import { GridColumn } from '../../models/grid-definition.model';
import { BaseComponent } from './base.component'


@Component({
    selector: 'd3s-load-item-tile',
    templateUrl: './load-item.tile.html',
    providers: [LoadService]
})

export class LoadItemTile extends BaseComponent implements OnChanges {
    @Input() id: number;
    @Input() title: string = "Load Details";
    
    columns: GridColumn[];
    items: any[];


    constructor(private loadService: LoadService) {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'id') {
                this.load();                
            }
        }

        this.load();
    }

    exportErrors(): void {
        if (this.id == null)
            return;

        this.loadService.getLoadErrorsXls(this.id);
    }

    exportOriginal(): void {
        if (this.id == null)
            return;

        this.loadService.getLoadOriginalXls(this.id);
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
