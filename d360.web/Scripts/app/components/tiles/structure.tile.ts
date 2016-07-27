///<reference path="../../es6-shim.d.ts"/>
import { Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';


@Component({
    selector: 'd3s-structure-tile',
    template: ``,
})

export class StructureTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: number;

    private isLoading = false;

    constructor() {
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            //if (p == 'objectType') {
            //    this.objectType = changes['objectType'].currentValue;
            //}
            //if (p == 'objectID') {
            //    this.objectID = changes['objectID'].currentValue;
            //}
        }

        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = false;

    }
}
