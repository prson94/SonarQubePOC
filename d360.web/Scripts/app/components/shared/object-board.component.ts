///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-board',
    template: `
            <header>Board</header>
            <span class="governance-value">{{commentCount}}</span>
            
        `
})

export class ObjectBoardComponent extends BaseComponent {    
    @Input() commentCount: number = 0;

    constructor() {
        super();
    }
    
}