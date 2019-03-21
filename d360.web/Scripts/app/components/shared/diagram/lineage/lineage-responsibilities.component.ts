import {Component, Input, OnChanges, OnInit} from '@angular/core';

import {Responsibility} from '../../../../models/lineage.model';

import {DiagramService} from '../../../../services/diagram.service';
import {ObjectDetailService} from '../../../../services/object-detail.service';

import {BaseComponent} from '../../base.component';

@Component({
    selector: 'd3s-lineage-responsibilities',
    templateUrl: './lineage-responsibilities.component.html',
    providers: [DiagramService, ObjectDetailService]
})

export class LineageResponsibilitiesComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() assetId: number;
    isLoading = false;

    @Input() objectType: string;
    @Input() objectId: number;

    items: Responsibility[] = [];

    constructor(
        private diagramService: DiagramService,
        private objectDetailService: ObjectDetailService
    ) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() {
    }

    private load() {
        // if the object type and objectid is passed and the assetid is null lookup the assetid then load responsibilities
        if (this.objectType && this.objectId != undefined && this.assetId == null) {
            this.objectDetailService.getObject(this.objectId, this.objectType)
                .then(data => {
                    this.assetId = data.AssetID;
                    this.loadResponsibilities();
                })
        } else {
            this.loadResponsibilities();
        }
    }

    private loadResponsibilities() {
        if (this.assetId == null || this.assetId < 1) {
            this.items = [];
            return;
        }

        this.isLoading = true;
        this.diagramService.getLineageResponsibilities(this.assetId).subscribe(
            data => {
                this.items = data;

                this.isLoading = false;
            }
        );
    }
}
