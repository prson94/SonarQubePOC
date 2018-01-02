import { Component, NgZone, OnInit, Output, EventEmitter, Input, OnChanges, AfterViewChecked, ElementRef, ViewChild } from '@angular/core';
import { FieldType } from '../../../../models/fields.model';
import { WorkflowService } from '../../../../services/workflow.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-template-tool',
    providers: [WorkflowService],
    template: `
<div class="ql-custom-field-tool" #cont style="display: inline-block">
    <select style="font-size: 1em; font-weight: normal; display: block !important;" #sel [ngModel]="selected" (ngModelChange)="clickItem($event)">
        <option value="none" disabled>Append field value...</option>
        <option *ngFor="let f of fields" [value]="f.value" style="color:#000;">{{f.label}}</option>
    </select>
</div>
`
})

export class WorkflowTemplateToolComponent implements OnInit, AfterViewChecked {
    @Input() objectType: string;
    @Input() objectId: number;
    @Output() onItemClick = new EventEmitter();
    @ViewChild('sel') select; 
    @ViewChild('cont') container;

    private fields = [];

    private defaultFields = [
        { value: '[OBJECT_NAME]', label: 'Object Name' },
        { value: '[SCORE]', label: 'Object Score' },
        { value: '[WORKFLOW_INITIATOR]', label: 'Workflow Initiator Name' },
        { value: '[ACTION_DETAILS]', label: 'Action Details' },
    ];

    private selected = "none";

    constructor(private workflowService: WorkflowService) {
    }

    ngOnInit() {
        this.fields = _.cloneDeep(this.defaultFields);
    }

    ngOnChanges() {
        //console.log('ngOnChanges', this.objectType, this.objectId);
        if (this.objectType != null && this.objectId != null)
            this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType)
                .then(r => {
                    this.fields = [];
                    this.fields = _.cloneDeep(this.defaultFields);
                    r.forEach(f => {
                        this.fields.push({
                            value: '[FIELD'+ f.ID +']',
                            label: 'Field :: ' + f.Name
                        });
                    });
                });
    }

    ngAfterViewChecked() {
        //Workaround for quill until primeng supports better quill API access

        //quill generates 'display: none' on <select> nodes
        //update it here
        this.select.nativeElement.style.display = 'inline-block';

        //remove auto-generated spans
        for (let i = 0; i < this.container.nativeElement.childNodes.length; i++) {
            let node = this.container.nativeElement.childNodes[i];

            if (node.tagName == 'SPAN') {
                this.container.nativeElement.removeChild(node);
            }
        }
    }

    clickItem(e: any) {
        if (e == "none")
            return;
        this.onItemClick.emit(e);
        this.selected = "none";
        this.select.nativeElement.value = "none";
    }

}