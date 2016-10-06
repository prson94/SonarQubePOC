
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { TreeNode } from 'primeng/primeng';
import { FusionAttributeType, FusionType } from '../../models/fusion.model';
import { FusionService } from '../../services/fusion.service';
import { FormMode } from '../../models/form.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-attributes-tile',
    templateUrl: './fusion-attributes.tile.html',
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

    newFusion: FusionAttributeType;

    constructor(private fusionService: FusionService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        //console.log('ngOnChanges');
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

    edit() {
        this.newFusion = _.cloneDeep(this.selectedRow.data);
        this.formMode = FormMode.Editing;
    }

    add(id: number) {
        this.newFusion = new FusionAttributeType();
        this.newFusion.FusionTypeID = this.fusionType.ID;
        if (id)
            this.newFusion.ParentID = id;
        else
            this.newFusion.ParentID = null;

        this.formMode = FormMode.Adding;
        //this.newFusion.
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        this.isLoading = true;

        if (this.formMode == FormMode.Editing) {
            this.fusionService.putFusionAttributeType(this.newFusion)
                .then(data => {
                    this.isLoading = false;
                    this.formMode = FormMode.Default;
                    this.load();
                });
        } else if (this.formMode == FormMode.Adding) {
            this.fusionService.postFusionAttributeType(this.newFusion)
                .then(data => {
                    this.isLoading = false;
                    this.formMode = FormMode.Default;
                    this.load();
                });
        }
    }

}


