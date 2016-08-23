///<reference path="../../es6-shim.d.ts"/>
import { Component, Input } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-health',
    template: `
            <header>Health</header>
            <span class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}">{{score}}%</span>
        `
})

export class ObjectHealthComponent extends BaseComponent {    
    @Input() score: number = 0;

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

}