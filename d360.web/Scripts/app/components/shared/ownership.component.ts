///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';


@Component({
    selector: 'd3s-ownership',
    template: `
            <div>Aw Snap its a ownership!</div>
        `
})

export class OwnershipComponent {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    constructor() {

    }


}