import { Component, Input, Output, EventEmitter, OnChanges, SimpleChange, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'd3s-object-issues',
    template: `
            <div (click)="toggleDetails()" >                
                <div class="governance-value" [ngClass]="{'governance-value-fail':isFail(), 'governance-value-warning': isWarning(), 'governance-value-pass': isPass()}">
                    {{issueCount}}
                    <span class="title">Open Actions</span>
                </div>
                <div class="governance-note">{{lastIssueMessage()}}</div>
            </div>
        `,
    changeDetection: ChangeDetectionStrategy.OnPush,
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
            return "No actions assigned.";
        }
        
        var years = this.dateDiff.getUTCFullYear() - 1970;

        if (years > 0) return "Last action came in " + years + " years ago.";

        var months = this.dateDiff.getUTCMonth();

        if (months > 0) return "Last action came in " + months + " months ago.";

        var days = this.dateDiff.getUTCDate() - 1;

        if (days > 0) return "Last action came in " + days + " days ago.";

        var hours = this.dateDiff.getUTCHours();

        if (hours > 0) return "Last action came in " + hours + " hours ago.";

        var minutes = this.dateDiff.getUTCMinutes();

        if (minutes > 0) return "Last action came in " + minutes + " minutes ago.";
                
        return "Last action was a moment ago.";
    }
}