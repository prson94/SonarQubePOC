///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter} from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-board-details',
    template: `
            <div class="row">
                <div class="col s12">
                    <header>Social for {{objectName}}</header>
                    Show social control for this object
                </div>                
            </div>
            
        `
})

export class ObjectBoardDetailsComponent extends BaseComponent {    
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;

    constructor() {
        super();
    }    

}