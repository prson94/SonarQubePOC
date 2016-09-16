///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-statistics',
    template: ` 
                <div class="tile tile-detail">
                    <header>Statistics</header>
                </div>
                `
})

export class FusionStatisticsComponent extends BaseComponent implements OnInit {
    constructor() {
        super();
    }

    ngOnInit() {

    }
};