import {Component, Input, OnChanges, OnInit} from '@angular/core';
import {DiagramService} from '../../../../services/diagram.service';
import {SourceRule} from '../../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-source-rules',
    templateUrl: './lineage-source-rules.component.html',
    providers: [DiagramService]
})

export class LineageSourceRulesComponent implements OnInit, OnChanges {
    @Input() source: string;
    @Input() sourceId: number;
    @Input() target: string;
    @Input() targetId: number;
    @Input() focal: string;
    @Input() focalId: number;

    items: SourceRule[] = [];
    isLoading = false;

    constructor(
        private diagramService: DiagramService
    ) {
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

        if (this.focal == null || this.focalId == null) {
            this.diagramService.getLineageSourceRules(
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
        } else {
            this.diagramService.getLineageSourceRulesFocal(
                this.focal,
                this.focalId,
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
}
