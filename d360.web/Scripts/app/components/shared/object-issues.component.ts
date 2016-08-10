///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';


@Component({
    selector: 'd3s-object-issues',
    template: `
            <header>Issues</header>
        `
})

export class ObjectIssuesComponent {
    @Input() objectType: string;
    @Input() objectID: number;

    constructor() {

    }

}