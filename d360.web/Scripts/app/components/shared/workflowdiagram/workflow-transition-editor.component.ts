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
            <select [ngModel]="transition.transitionType" (ngModelChange)="changeType($event)" style="width: 95%">
                <option *ngFor="let t of transitionTypes" [value]="t.ID">{{t.Name}}</option>
            </select>
        </div>
    </div>
</div>
<div [ngSwitch]="transition.transitionType">
    <div *ngSwitchCase="TransitionType.Condition" class="row">
        <div class="col s12">
            <header>&nbsp;<d3s-tile-actions hasAdd="true" (addClick)="add()"></d3s-tile-actions></header>
            <p-dataTable [value]="transition.condition" selectionMode="single">
                <p-column field="@FieldName" header="Field Name"></p-column>
                <p-column field="@Operator" header="Operator"></p-column>
                <p-column field="@Value" header="Value"></p-column>
                <p-column>
                    <template let-item="rowData" pTemplate type="body">
                        <div class="RowTools">
                            <a style="cursor:pointer;" (click)="remove(item)"><i class="fa fa-trash"></i></a>
                        </div>
                    </template>
                </p-column>
            </p-dataTable>
            
            <d3s-workflow-condition-editor
                *ngIf="showAddCondition"
                [objectId]="objectId" 
                [objectType]="objectType" 
                (onSave)="addCondition($event)" 
                (onClose)="showAddCondition = false;">
            </d3s-workflow-condition-editor>

        </div>
    </div>
    <div *ngSwitchCase="TransitionType.Timer" class="row">
        <div class="col s12">
            <div class="FieldName">Days</div>
            <div>
                <input type="number" [ngModel]="transition.settings.TimerInterval" (ngModelChange)="transition.settings.TimerInterval = $event; transitionChange.emit(transition)" style="width: 95%" />
            </div>
        </div>
    </div>
</div>
`
})

export class WorkflowTransitionEditorComponent extends BaseComponent implements OnInit {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() transition: LinkModel;
    @Output() transitionChange = new EventEmitter();

    private originalTransition: LinkModel;
    private transitionTypes: TransitionTypeInfo[] = [];
    private showAddCondition = false;

    TransitionType = TransitionType;

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

    add() {
        this.showAddCondition = true;
    }

    remove(e: any) {
        let i = this.transition.condition.findIndex(c => c == e);
        this.transition.condition.splice(i, 1);
        this.transitionChange.emit(this.transition);
    }

    addCondition(e: any) {
        this.transition.condition.push(e);
        this.showAddCondition = false;
        this.transitionChange.emit(this.transition);
    }

    changeType(e: any) {
        this.transition.transitionType = e;
        this.transitionChange.emit(this.transition);
    }
}