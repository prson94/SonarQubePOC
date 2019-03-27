import {Component, Input, OnChanges, OnInit} from '@angular/core';
import {DiagramService} from '../../../../services/diagram.service';
import {TechnicalRelation} from '../../../../models/lineage.model';
import {BaseComponent} from '../../base.component';

@Component({
    selector: 'd3s-lineage-technical',
    templateUrl: './lineage-technical-relationships.component.html',
    providers: [DiagramService]
})

export class LineageTechnicalRelationshipsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() source: string;
    @Input() sourceId: number;
    @Input() target: string;
    @Input() targetId: number;

    isLoading = false;
    items: TechnicalRelation[] = [];

    constructor(private diagramService: DiagramService) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() {
    }

    load() {
        if (this.source == null || this.sourceId == null || this.target == null || this.targetId == null) {
            this.items = [];
            return;
        }

        this.isLoading = true;
        this.diagramService.getLineageTechnicalRelationships(
            this.source,
            this.sourceId,
            this.target,
            this.targetId
        ).subscribe(
            data => {
                this.items = data;

                this.isLoading = false;
            }
        );
    }
}
