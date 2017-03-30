import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowListItem,
    WorkflowDiagramModel,
    WorkflowDiagramLink,
    LinkModel,
    TransitionType,
    TransitionTypeInfo,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-transition-editor',
    providers: [WorkflowService],
    template: `
<div class="row">
    <div class="col s12">
        <div class="FieldName">Name</div>
        <div>
            <input type="text" style="width: 95%" [ngModel]="transition.name" (ngModelChange)="transition.name = $event; transitionChange.emit(transition)" />
        </div>
    </div>
    <div class="col s12">
        <div class="FieldName">Transition Type</div>
        <div>
            <select [ngModel]="transition.transitionType" (ngModelChange)="transition.transitionType = $event; transitionChange.emit(transition)" style="width: 95%">
                <option *ngFor="let t of transitionTypes" [value]="t.ID">{{t.Name}}</option>
            </select>
        </div>
    </div>
</div>
<div class="row">

</div>
`
})

export class WorkflowTransitionEditorComponent extends BaseComponent implements OnInit {
    @Input() transition: LinkModel;
    @Output() transitionChange = new EventEmitter();

    private originalTransition: LinkModel;
    private transitionTypes: TransitionTypeInfo[] = [];

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        console.log(this.transition);
        this.originalTransition = _.cloneDeep(this.transition);
        this.workflowService.getTransitionTypes()
            .then(r => {
                this.transitionTypes = r;
            });
    }
}