import { Component, NgZone, OnInit, Output, EventEmitter, Input, OnChanges, AfterViewChecked, ElementRef, ViewChild } from '@angular/core';
import * as _ from 'lodash';

import { FieldType } from '../../../../models/fields.model';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
    selector: 'd3s-workflow-template-tool',
    providers: [WorkflowService],
    templateUrl: './workflow-template-tool.component.html'
})

export class WorkflowTemplateToolComponent implements OnInit, AfterViewChecked {
    @Input() objectType: string;
    @Input() objectId: number;
    @Output() onItemClick = new EventEmitter();
    @Input() issueObject: string;
    @ViewChild('sel') select;
    @ViewChild('cont') container;

    private fields = [];

    private defaultFields = [
        { value: '[OBJECT_NAME]', label: 'Object Name' },
        { value: '[OBJECT_TYPE]', label: 'Object Type' },
        { value: '[SCORE]', label: 'Object Score' },
        { value: '[WORKFLOW_INITIATOR]', label: 'Workflow Initiator Name' },
        { value: '[ACTION_DETAILS]', label: 'Action Details' },
        { value: '[RECIPIENT_TYPE]', label: 'Recipient Type' },
        { value: '[RECIPIENT_RESPONSIBILITY]', label: 'Recipient Responsibility' },
    ];

    private relationshipFields = [
        { value: '[REL_SUBJECT_NAME]', label: 'Relationship Subject Name' },
        { value: '[REL_OBJECT_NAME]', label: 'Relationship Object Name' },
    ];

    private selected = "none";

    constructor(private workflowService: WorkflowService) {
    }

    ngOnInit() {
        this.fields = _.cloneDeep(this.defaultFields);
    }

    ngOnChanges() {
        if (this.objectType != null && this.objectId != null)
            this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType, true, this.issueObject)
                .subscribe(r => {
                    this.fields = [];
                    this.fields = _.cloneDeep(this.defaultFields);

                    if (this.objectType == 'IntersectType')
                        this.fields = this.fields.concat(_.cloneDeep(this.relationshipFields));
                
                    r.forEach(f => {
                        let fieldType = f.Object == "IssueType" ? "Action Field" : "Asset Field";

                        this.fields.push({
                            value: '[FIELD' + f.ID + ']#[' + fieldType + ' :: ' + f.Name +']',
                            label: fieldType + ' :: ' + f.Name
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
        let f = e.split('#');
        if (f.length == 2)
            this.onItemClick.emit(f[1]);
        else
            this.onItemClick.emit(f[0]);
        this.selected = "none";
        this.select.nativeElement.value = "none";
    }

}
