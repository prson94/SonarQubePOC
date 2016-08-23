///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-issues',
    template: `
            <div (click)="toggleDetails()" >
                <header>Issues</header>
                <div class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}">{{issueCount}}</div>            
            </div>
        `
})

export class ObjectIssuesComponent extends BaseComponent {
    @Input() issueCount: number = 0;

    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();

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

    toggleDetails() {        
        this.showDetails = !this.showDetails;        
        this.showDetailsChange.emit(this.showDetails);
    }
}