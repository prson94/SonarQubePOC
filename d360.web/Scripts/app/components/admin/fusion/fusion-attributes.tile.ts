import {Input, Component, OnChanges, SimpleChange} from '@angular/core';
import { TreeNode } from 'primeng/api';
import { FusionType} from '../../../models/fusion.model';
import { AssetTypeEditorModel } from "../../../models/asset.model";
import { FusionService } from '../../../services/fusion.service';
import { ObjectStyleService } from '../../../services/object-style.service';
import { FormMode } from '../../../models/form.model';
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

    fusionAttributeTypes: any;
    selectedRow: TreeNode;

    editorModel: AssetTypeEditorModel;

    constructor(
        private fusionService: FusionService,
        private objectStyleService: ObjectStyleService
    ) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'fusionType') {
                this.load(null);
            }
        }
    }

    load(id: number): void {
        this.isLoading = true;

        if (this.fusionType == null) {
            this.formMode = FormMode.Default;
            this.fusionAttributeTypes = null;
            this.selectedRow = null;
            this.isLoading = false;
            return;
        }

        this.fusionService.getFusionAttributeTypeTree(this.fusionType.ID).subscribe(
            data => {
                this.fusionAttributeTypes = data;

                if (id) {
                    this.selectedRow = this.fusionAttributeTypes.filter(i => i.data.ID == id)[0];
                } else {
                    this.selectedRow = this.fusionAttributeTypes[0];
                }

                this.isLoading = false;
            }
        );
    }

    edit() {
        this.formMode = FormMode.Editing;
    }

    add() {
        this.formMode = FormMode.Adding;
    }

    delete() {
        this.formMode = FormMode.Deleting;
    }

    save() {
        this.formMode = FormMode.Default;
        this.load(null);
    }
}
