///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import { DataTable, Column, Accordion, AccordionTab } from 'primeng/primeng';
import { ObjectDetailService } from '../../services/object-detail.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { DetailRow, DetailField, DetailModel, IObjectDetailService } from '../../models/object-detail.model';
import { ArtifactDefnintionComponent } from '../artifact/artifact-definition.component';
import { Artifact } from '../../models/artifacts.model';
import { SynonymsTile } from '../tiles/synonyms.tile';
import { AttributesTile } from '../tiles/attributes.tile';

declare var CompanySettings;


@Component({
    selector: 'd3s-artifact-detail',
    directives: [DataTable, Column, ArtifactDefnintionComponent, Accordion, AccordionTab, ObjectDetailTile, SynonymsTile, AttributesTile],
    templateUrl: 'scripts/app/components/artifact/artifact-detail.component.html',
    providers: [ObjectDetailService],
})

export class ArtifactDetailComponent implements OnChanges {
    @Input() artifact: Artifact;

    private isLoading = false;

    constructor(private objectDetailService: ObjectDetailService) {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            //if (p == 'objectType') {
            //    this.objectType = changes['objectType'].currentValue;
            //}
            //if (p == 'objectID') {
            //    this.objectID = changes['objectID'].currentValue;
            //}
        }

        this.load();
    }

    load(): void {

        if (this.artifact == null)
            return;

        this.objectDetailService.getObjectDetail(this.artifact.ID, 'Artifact')
            .then(d => {
                //console.log(d);
            });

        this.isLoading = false;
    }
}
