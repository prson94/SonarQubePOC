import {Component, Input, OnChanges, SimpleChange} from '@angular/core';
import {TreeNode} from 'primeng/primeng';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

import {FusionType} from '../../../models/fusion.model';
import {AssetTypeEditorModel} from "../../../models/asset.model";
import {FusionService} from '../../../services/fusion.service';
import {FormHelper, FormMode} from '../../../models/form.model';

import {ObjectStyleService} from '../../../services/object-style.service';

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

    editorModel: AssetTypeEditorModel;

    destroySubject$: Subject<void> = new Subject();

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

        this.fusionService
            .getFusionAttributeTypes(this.fusionType.ID)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                data => {
                    this.fusionAttributeTypes = FormHelper.formTree(data);

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
