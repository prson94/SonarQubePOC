///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { IssuesService } from '../../services/index';

@Component({
    selector: 'd3s-object-issue-details',
    template: `
            <div class="row" *ngIf="!isLoading && issues.length > 0">
                <header>Open Issues</header>
                <div class="col s12">
                    <p-dataTable  scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="issues" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected">
                        <p-column field="Issue" header="Issue" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-issue="rowData">
                                <span [innerHtml]="issue?.Issue"></span>
                            </template>
                        </p-column>
                        <p-column field="ResourceName" header="Reported By" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                        <p-column field="DateStarted" header="Created" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-data="rowData">
                                <span>{{data.DateStarted | date: 'medium'}}</span>
                            </template>
                        </p-column>
                        <p-column field="ActivityName" header="Status" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                    </p-dataTable>   
                </div>
            </div>
            <div class="row" *ngIf="!isLoading && issues.length == 0">
                <div class="col s12 center">
                    No issues exist.
                </div>
            </div>
        `,
    providers: [IssuesService]
})

export class ObjectIssueDetailsComponent extends BaseComponent implements OnInit {
    private issues: any[] = [];
    private selected: any;
    private loaded: boolean = false;
    @Input() objectID: number;
    @Input() objectType: string;
    @Input() objectName: string;


    constructor(private issuesService: IssuesService) {
        super();
    }

    ngOnInit() {
        if (!this.loaded)
            this.loadIssues();
    }

    private loadIssues() {
        this.isLoading = true;
        this.issuesService.getIssues(this.objectID, this.objectType)
            .then(result => {
                this.issues = result;
                if (this.issues.length && this.issues.length > 0) this.selected = this.issues[0];
                this.isLoading = false;
                this.loaded = true;
            });
    }
}