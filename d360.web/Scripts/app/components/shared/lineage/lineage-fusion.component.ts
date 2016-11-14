import { Component, Input, OnInit, OnChanges } from '@angular/core';
import { LineageService } from '../../../services/index';

@Component({
    selector: 'd3s-lineage-fusion',
    template: `
        <d3s-loading [isLoading]="isLoading"></d3s-loading>
        
    `,
    providers: [LineageService]
})

export class LineageFusionComponent implements OnInit, OnChanges {

    isLoading = false;

    constructor(private lineageService: LineageService) { }

    ngOnChanges() {
        this.load();
    }

    ngOnInit() { }

    load() {
        this.isLoading = true;
    }
}