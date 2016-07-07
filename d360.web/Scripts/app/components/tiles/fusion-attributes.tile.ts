///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { Column, TreeTable, TreeNode } from 'primeng/primeng';
import { FusionAttributeType, FusionType } from '../../models/fusion.model';
import { FusionService } from '../../services/fusion.service';
import { TileActionsComponent } from './tile-actions.component';
import { FieldDefinitionTile } from './field-definition.tile';
import { FieldTypeForm } from '../forms/field-type.form';
import { DeleteForm } from '../forms/delete.form';
import { FormMode } from '../../models/form.model';

@Component({
    selector: 'd3s-fusion-attributes-tile',
    directives: [TreeTable, Column, TileActionsComponent, FieldDefinitionTile],
    templateUrl: 'scripts/app/components/tiles/fusion-attributes.tile.html',
    providers: [FusionService]
})

export class FusionAttributesTile implements OnChanges {
    @Input() fusionType: FusionType;
    @Input() title: string = 'Structure';

    isLoading = false;
    formMode: FormMode = FormMode.Default;
    FormMode = FormMode;

    fusionAttributeTypes: TreeNode[];
    selectedRow: TreeNode;

    constructor(private fusionService: FusionService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        console.log('ngOnChanges');
        for (let p in changes) {
            if (p == 'fusionType') {
                this.load();
            }

        }
    }

    load(): void {
        this.isLoading = true;
        if (this.fusionType == null) {
            this.formMode = FormMode.Default;
            this.fusionAttributeTypes = null;
            this.selectedRow = null;
            this.isLoading = false;
            return;
        }
        this.fusionService.getFusionAttributeTypeTree(this.fusionType.ID)
            .then(data => {
                this.fusionAttributeTypes = data;
                this.selectedRow = this.fusionAttributeTypes[0];
                this.isLoading = false;
            });
    }


}


