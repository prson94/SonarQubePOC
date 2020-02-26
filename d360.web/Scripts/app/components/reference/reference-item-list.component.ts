import { Input, Component, EventEmitter, Output, OnInit, OnDestroy, ChangeDetectionStrategy, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetService } from '../../services/asset.service';
import { ReferenceItemType } from '../../models/reference.model';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { GridColumn, GridField } from '../../models/grid-definition.model';

@Component({
    selector: 'd3s-reference-item-list',
    templateUrl: './reference-item-list.component.html',
    providers: [AssetService, GridDefinitionService]
})

export class ReferenceItemGridComponent extends BaseComponent implements OnInit, OnChanges {

    constructor(
        private assetService: AssetService,
        private gridDefinitionService: GridDefinitionService
    ) {
        super();
    }

    @Input() assetTypeUid: string;
    private items: any[] = [];

    columns: GridColumn[] = [];
    fields: GridField[] = [];

    private selected: any;
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    add() {
        this.selected = null;
        this.showEditor = true;
    }

    private title: string = 'Items';

    ngOnChanges(changes: SimpleChanges) {
        if (changes.assetTypeUid.currentValue != changes.assetTypeUid.previousValue) {
            this.load();
        }
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        console.log("loading");
        if (!this.assetTypeUid)
            return;

        this.isLoading = true;

        this.gridDefinitionService.getGridDefinition(this.assetTypeUid, 'ReferenceItemType').subscribe(
            result => {
                this.columns = result.Columns;
                this.fields = result.Fields;

                this.assetService.getAssets(this.assetTypeUid, null).subscribe(result => {
                    console.log(result);
                    this.items = result.items;
                    if (this.items.length > 0) {
                        this.selected = this.items[0];
                    }
                    this.isLoading = false;
                });
            }
        ); 
    }

    private export() {
        console.log("exporting");
    }

}
