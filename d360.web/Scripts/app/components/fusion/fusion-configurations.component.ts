///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-fusion-configuration',
    template: ` 
                <div class="tile tile-detail">
                    <header>Configuration</header>
                </div>
                `
})

export class FusionConfigurationComponent extends BaseComponent implements OnInit {
    constructor() {
        super();
    }

    ngOnInit() {
        
    }
};