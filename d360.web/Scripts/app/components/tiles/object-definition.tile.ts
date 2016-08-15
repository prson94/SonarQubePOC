///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { DataTable, Column, Accordion, AccordionTab } from 'primeng/primeng';
import { ObjectDetailService } from '../../services/object-detail.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { SynonymsTile } from '../tiles/synonyms.tile';
import { AttributesTile } from '../tiles/attributes.tile';
import { SimpleAccordion } from '../parts/simple-accordion.part';
import { StructureTile } from '../tiles/structure.tile';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { FormMode } from '../../models/form.model';
import { DynamicEditorComponent } from '../shared/dynamic-editor.component';
import { ObjectDetail } from '../../models/object-detail.model';


@Component({
    selector: 'd3s-object-definition-tile',
    directives: [DataTable, Column, Accordion, AccordionTab, ObjectDetailTile, SynonymsTile, AttributesTile, SimpleAccordion, StructureTile, TileActionsComponent, DynamicEditorComponent],
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
