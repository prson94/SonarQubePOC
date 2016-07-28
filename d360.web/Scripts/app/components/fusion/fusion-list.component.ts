///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';

@Component({
    selector: 'd3s-fusion-list',
    template: ` Fusion List
                `
})

export class FusionListComponent extends BaseComponent implements OnInit {
    constructor(protected titleService: Title) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, `D3S - Fusion`);
    }
};