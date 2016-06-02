///<reference path="../es6-shim.d.ts"/>
import {Input, Output, Component, OnInit} from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import { WorkflowItem } from '../models/workflow.model';

@Component({
    selector: 'workflow-item-editor',
    templateUrl: 'scripts/app/editors/workflow-item.editor.html',
    viewProviders: [HTTP_PROVIDERS]
})

export class WorkflowItemEditor implements OnInit {
    @Input() workflowItem: WorkflowItem;

    private isLoading = false;

    columns: number;
    http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.load();
    }

    private load(): void {

        if (this.workflowItem == null)
            return;
        this.isLoading = true;

        
        //if (this.objectType && this.objectID)
        //    this.http.get('/api/' + this.objectType + '/' + this.objectID + '/detail').map(data => data.json()).subscribe(data => {
        //        this.rows = [];

        //        //console.log(data);

        //        this.columns = data.columns;
        //        data.rows.forEach(r => this.rows.push(r));

        //        this.isLoading = false;
        //    });
    }
}
