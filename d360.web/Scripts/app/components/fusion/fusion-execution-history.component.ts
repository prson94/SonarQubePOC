///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-execution-history',
    template: ` 
                <div class="tile tile-detail">
                    <header>Execution History</header>
                </div>
                `
})

export class FusionExecutionHistoryComponent extends BaseComponent implements OnInit {
    constructor() {
        super();
    }

    ngOnInit() {

    }
};