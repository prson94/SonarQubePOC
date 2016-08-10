///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';


@Component({
    selector: 'd3s-object-health',
    template: `
            <header>Health</header>
        `
})

export class ObjectHealthComponent {
    @Input() objectType: string;
    @Input() objectID: number;

    constructor() {

    }

}