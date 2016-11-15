import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../services/index';

@Component({
    selector: 'd3s-lineage-object-detail',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        <div [hidden]="isLoading" [innerHtml]="data"></div>
    `,
    providers: [DiagramService]
})

export class LineageObjectDetailComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectId: number;

    data: any = null;
    isLoading = false;

    constructor(private diagramService: DiagramService) { }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {
        this.isLoading = true;
        this.diagramService.getLineageObjectDetail(this.objectType, this.objectId)
            .then(data => {
                //console.log(data);
                this.data = data._body;
                this.isLoading = false;
            });
    }
}