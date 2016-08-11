///<reference path="../../es6-shim.d.ts"/>
import {Component,Input} from '@angular/core';


@Component({
    selector: 'd3s-dashboard',
    template: `
            <div>Aw Snap its a dashboard!</div>
        `    
})

export class DashboardComponent {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    constructor() {
        
    }

    
}