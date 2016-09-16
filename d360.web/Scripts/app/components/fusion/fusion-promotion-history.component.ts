///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-promotion-history',
    template: ` 
                <div class="tile tile-detail">
                    <header>Promotion History</header>
                </div>
                `
})

export class FusionPromotionHistoryComponent extends BaseComponent implements OnInit {
    constructor() {
        super();
    }

    ngOnInit() {

    }
};