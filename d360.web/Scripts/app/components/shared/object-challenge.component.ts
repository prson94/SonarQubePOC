///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';


@Component({
    selector: 'd3s-object-challenge',
    template: `
            <header>Challenge</header>
        `
})

export class ObjectChallengeComponent {
    @Input() objectType: string;
    @Input() objectID: number;

    constructor() {

    }

}