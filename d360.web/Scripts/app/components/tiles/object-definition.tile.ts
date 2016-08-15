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
declare var CompanySettings;


@Component({
    selector: 'd3s-object-definition-tile',
    directives: [DataTable, Column, Accordion, AccordionTab, ObjectDetailTile, SynonymsTile, AttributesTile, SimpleAccordion, StructureTile],
    templateUrl: 'scripts/app/components/tiles/object-definition.tile.html',
    providers: [ObjectDetailService],
})

export class ObjectDefinitionTile implements OnChanges {
    @Input() objectType: string = 'Artifact';
    @Input() objectID: number;


    private isLoading = false;

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {

        }

        this.load();
    }

    load(): void {

        if (this.objectID == null || this.objectType == null)
            return;

        this.objectDetailService.getObjectDetail(this.objectID, this.objectType)
            .then(d => {
                //console.log(d);
            });

        this.isLoading = false;
    }
}
