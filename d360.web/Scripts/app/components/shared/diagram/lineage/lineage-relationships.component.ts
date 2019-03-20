import {Component, Input, OnChanges, OnInit} from '@angular/core';

import {RelationItem} from '../../../../models/lineage.model';

import {DiagramService} from '../../../../services/diagram.service';

import {BaseComponent} from '../../base.component';

@Component({
    selector: 'd3s-lineage-relations',
    templateUrl: './lineage-relationships.component.html',
    providers: [DiagramService]
})

export class LineageRelationshipsComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectId: number;
    isLoading = false;

    items: RelationItem[] = [];

    constructor(private diagramService: DiagramService) {
        super();
    }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() {
    }

    load() {

        if (this.objectType == null || this.objectId == null) {
            this.items = [];

            return;
        }

        this.isLoading = true;

        this.diagramService.getRelations(this.objectType, this.objectId).subscribe(
            data => {
                this.items = data;

                this.isLoading = false;
            }
        );
    }
}
