///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';


@Component({
    selector: 'd3s-object-board',
    template: `
            <header>Board</header>
        `
})

export class ObjectBoardComponent {
    @Input() objectType: string;
    @Input() objectID: number;

    constructor() {
        
    }
    
}