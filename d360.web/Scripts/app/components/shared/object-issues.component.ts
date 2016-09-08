///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, Output, EventEmitter, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-issues',
    template: `
            <div (click)="toggleDetails()" >
                <header>Issues</header>
                <div class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}">{{issueCount}}</div>            
                <div class="row">
                    <div class="col s12">
                        {{lastIssueMessage()}}
                    </div>
                </div>
            </div>
        `
})

export class ObjectIssuesComponent extends BaseComponent implements OnChanges  {
    @Input() issueCount: number = 0;
    @Input() lastIssueDate: string;

    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();

    private dateDiff: Date;

    constructor() {
        super();
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.lastIssueDate) {
            this.dateDiff = new Date(Date.now() - Date.parse(this.lastIssueDate));
        }

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

    private lastIssueMessage() {
        if (!this.lastIssueDate) {
            return "No issues raised.";
        }
        
        var years = this.dateDiff.getUTCFullYear() - 1970;

        if (years > 0) return "Last issue came in " + years + " years ago.";

        var months = this.dateDiff.getUTCMonth();

        if (months > 0) return "Last issue came in " + months + " months ago.";

        var days = this.dateDiff.getUTCDate() - 1;

        if (days > 0) return "Last issue came in " + days + " days ago.";

        var hours = this.dateDiff.getUTCHours();

        if (hours > 0) return "Last issue came in " + hours + " hours ago.";

        var minutes = this.dateDiff.getUTCMinutes();

        if (minutes > 0) return "Last issue came in " + minutes + " minutes ago.";
                
        return "Last discussion was a moment ago.";
    }
}