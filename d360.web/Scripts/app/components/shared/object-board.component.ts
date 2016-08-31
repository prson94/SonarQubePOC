///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter} from '@angular/core';
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

export class ObjectBoardComponent extends BaseComponent {    
    @Input() commentCount: number = 0;
    @Input() lastCommentDate: string;

    @Input() showDetails: boolean = false;
    @Output() showDetailsChange = new EventEmitter();

    constructor() {
        super();
    }

    toggleDetails() {
        this.showDetails = !this.showDetails;
        this.showDetailsChange.emit(this.showDetails);
    }


    private lastBoardMessage() {
        if (!this.lastCommentDate) {
            return "No comments.";
        }
        let lastDate = Date.parse(this.lastCommentDate);

        var diff = new Date(Date.now() - lastDate);

        var years = diff.getUTCFullYear() - 1970;

        if (years > 0) return "Last discussion was " + years + " years ago.";

        var months = diff.getUTCMonth();

        if (months > 0) return "Last discussion was " + months + " months ago.";

        var days = diff.getUTCDate() - 1;

        if (days > 0) return "Last discussion was " + days + " days ago.";

        var hours = diff.getUTCHours();

        if (hours > 0) return "Last discussion was " + hours + " hours ago.";

        var minutes = diff.getUTCMinutes();

        if (minutes > 0) return "Last discussion was " + minutes + " minutes ago.";

        return "Last discussion was a few seconds ago.";
    }
}