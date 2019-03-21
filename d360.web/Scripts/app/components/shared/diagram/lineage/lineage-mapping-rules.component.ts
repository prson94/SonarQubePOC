import {Component, EventEmitter, Input, OnChanges, OnInit, Output} from '@angular/core';
import {DiagramService} from '../../../../services/diagram.service';
import {BaseComponent} from '../../base.component';
import {MapItem} from '../../../../models/lineage.model';

@Component({
    selector: 'd3s-lineage-mapping-rules',
    templateUrl: './lineage-mapping-rules.component.html',
    providers: [DiagramService]
})

export class LineageMappingRulesComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() source: string;
    @Input() sourceId: number;
    @Input() target: string;
    @Input() targetId: number;
    @Output() onExpandClick = new EventEmitter();

    isLoading = false;
    items: MapItem[];

    constructor(private diagramService: DiagramService) {
        super();
        this.showSimpleFilter = true;
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

        this.diagramService.getLineageMapItems(
            this.source,
            this.sourceId,
            this.target,
            this.targetId
        ).subscribe(
            data => {
                this.items = data;

                if (this.items && this.items.length > 0) {
                    this.items.forEach(i => {
                        //for global filter
                        i.searchableSource = i.SourceName + ' ' + i.SourceType;
                        i.searchableTarget = i.TargetName + ' ' + i.TargetType;
                        i.searchablSourceFusion = i.SourceFusion + ' ' + i.SourceFusionAttribute + ' ' + i.SourceFusionAttributeType;
                        i.searchableTargetFusion = i.TargetFusion + ' ' + i.TargetFusionAttribute + ' ' + i.TargetFusionAttributeType;
                    });
                }

                this.isLoading = false;
            }
        );
    }

    export() {
        DiagramService.getLineageMapItemsExport(this.source, this.sourceId, this.target, this.targetId);
    }
}
