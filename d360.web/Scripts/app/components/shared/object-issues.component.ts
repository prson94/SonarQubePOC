///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-issues',
    template: `
            <header>Issues</header>
            <span class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}">{{issueCount}}</span>            
        `
})

export class ObjectIssuesComponent extends BaseComponent {
    @Input() issueCount: number = 0;

    constructor() {
        super();
    }

    private isWarning(): boolean {
        return this.issueCount > 0 && this.issueCount < 5;
    }

    private isPass(): boolean {
        return this.issueCount <= 0;
    }

    private isFail(): boolean {
        return this.issueCount >= 5;
    }
}