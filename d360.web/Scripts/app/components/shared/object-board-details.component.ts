///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter} from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-board-details',
    template: `
            <div class="row">
                <div class="col s12">
                    <header>Social for {{objectName}}</header>
                    <d3s-social-board [objectID]="objectID" [objectType]="objectType" [objectName]="objectName" [daysToLookBack]="daysToLookBack"></d3s-social-board>
                </div>                
            </div>
            
        `
})

export class ObjectBoardDetailsComponent extends BaseComponent {    
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() daysToLookBack: number = 7;

    constructor() {
        super();
    }    

}