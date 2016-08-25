///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter} from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-board',
    template: `
            <div (click)="toggleDetails()" >
                <header>Board</header>
                <span class="governance-value">{{commentCount}}</span>
                <div class="row">
                    <div class="col s12">
                        <!--Last discussion was xxx days ago-->&nbsp;
                    </div>
                </div>
            </div>            
        `
})

export class ObjectBoardComponent extends BaseComponent {    
    @Input() commentCount: number = 0;

    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();

    constructor() {
        super();
    }

    toggleDetails() {
        this.showDetails = !this.showDetails;
        this.showDetailsChange.emit(this.showDetails);
    }
    
}