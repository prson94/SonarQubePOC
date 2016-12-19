import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { DiagramService } from '../../../services/diagram.service';

@Component({
    selector: 'd3s-lineage-fusion',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        
    `,
    providers: [DiagramService]
})

export class LineageFusionComponent implements OnInit, OnChanges {

    isLoading = false;

    constructor(private diagramService: DiagramService) { }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {
        this.isLoading = true;
    }
}