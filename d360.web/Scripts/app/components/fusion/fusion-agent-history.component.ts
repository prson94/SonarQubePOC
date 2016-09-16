import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
        selector: 'd3s-fusion-agent-history',
        template: ` 
                <div class="tile tile-detail">
                    <header>Agent History</header>
                </div>
                `
})

export class FusionAgentHistoryComponent extends BaseComponent implements OnInit {
    constructor() {
        super();
    }

    ngOnInit() {

    }
};