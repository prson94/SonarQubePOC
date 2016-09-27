
import {Component, Input, Output, EventEmitter, OnChanges, SimpleChange} from '@angular/core';
import { BaseComponent } from '../shared/base.component';

@Component({
    selector: 'd3s-object-board',
    template: `
            <div (click)="toggleDetails()" >
                <header>Board</header>
                <span class="governance-value">{{commentCount}}</span>
                <div class="row">
                    <div class="col s12">
                        {{lastBoardMessage()}}
                    </div>
                </div>
            </div>            
        `
})

export class ObjectBoardComponent extends BaseComponent implements OnChanges {    
    @Input() commentCount: number = 0;
    @Input() lastCommentDate: string;

    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();

    private dateDiff: Date;

    constructor() {
        super();
    }

    

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.lastCommentDate) {
            this.dateDiff = new Date(Date.now() - Date.parse(this.lastCommentDate));
        }
    }

    toggleDetails() {
        this.showDetails = !this.showDetails;
        this.showDetailsChange.emit(this.showDetails);
    }


    private lastBoardMessage() {
        if (!this.lastCommentDate) {
            return "No comments.";
        }
        
        var years = this.dateDiff.getUTCFullYear() - 1970;

        if (years > 0) return "Last discussion was " + years + " years ago.";

        var months = this.dateDiff.getUTCMonth();

        if (months > 0) return "Last discussion was " + months + " months ago.";

        var days = this.dateDiff.getUTCDate() - 1;

        if (days > 0) return "Last discussion was " + days + " days ago.";

        var hours = this.dateDiff.getUTCHours();

        if (hours > 0) return "Last discussion was " + hours + " hours ago.";

        var minutes = this.dateDiff.getUTCMinutes();

        if (minutes > 0) return "Last discussion was " + minutes + " minutes ago.";

        return "Last discussion was a moment ago.";
    }
}