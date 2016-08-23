///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-health',
    template: `
            <header>Health</header>
            <div class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}" (click)="toggleDetails()">{{score}}%</div>
        `
})

export class ObjectHealthComponent extends BaseComponent {    
    @Input() score: number = 0;

    @Input() showDetails: boolean = false;    
    @Output() showDetailsChange = new EventEmitter();

    constructor() {
        super();
    }

    private isWarning(): boolean {
        return this.score < 80 && this.score > 60;
    }

    private isPass(): boolean {
        return this.score > 80;
    }

    private isFail(): boolean {
        return this.score < 60;
    }

    private toggleDetails() {        
        this.showDetails = !this.showDetails;        
        this.showDetailsChange.emit( this.showDetails );
    }
}