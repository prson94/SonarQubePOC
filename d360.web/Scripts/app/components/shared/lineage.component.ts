///<reference path="../../es6-shim.d.ts"/>
import {Component,Input} from '@angular/core';


@Component({
    selector: 'd3s-lineage',
    template: `
            <div>Aw Snap its a lineage!</div>
        `
})

export class LineageComponent {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    constructor() {

    }


}