import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
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
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { FormMode } from '../../../../models/form.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';

import * as _ from 'lodash';
import * as go from 'gojs';

@Component({
    selector: 'd3s-workflow-transition-editor',
    providers: [WorkflowService],
    templateUrl: './workflow-transition-editor.component.html'
})

export class WorkflowTransitionEditorComponent extends BaseComponent implements OnInit, OnDestroy, OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() transition: LinkModel;
    @Input() diagram: go.Diagram;
    @Output() transitionChange = new EventEmitter();

    private originalTransition: LinkModel;
    private transitionTypes: TransitionTypeInfo[] = [];
    private condition = null;

    TransitionType = TransitionType;

    private fieldsSub: any;
    private formFields: any[] = [];
    private formMode = FormMode.Default;

    FormMode = FormMode;


    constructor(private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnInit() {
        this.originalTransition = _.cloneDeep(this.transition);
        this.workflowService.getTransitionTypes()
            .subscribe(r => {
                this.transitionTypes = r;
            });

        this.filterFormFields();

        this.fieldsSub = this.workflowFieldsService.formFields$.subscribe(s => {
            this.filterFormFields();
            //console.log('(sub) transition editor form fields:', this.formFields);
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (!changes['transition'].isFirstChange() && changes['transition'].currentValue.key != changes['transition'].previousValue.key) {
            this.formMode = FormMode.Default;
            this.filterFormFields();
        } else if (!changes['transition'].isFirstChange()) {
            this.filterFormFields();
        }
    }

    ngOnDestroy() {
        this.fieldsSub.unsubscribe();
    }

    add() {
        this.condition = null;
        this.filterFormFields();
        this.formMode = FormMode.Adding;
    }

    remove(e: any) {
        let i = this.transition.condition.findIndex(c => c == e);

        if (e['@FormInputID'] != null)
            this.workflowFieldsService.deleteUsedField(this.transition.condition[i]['@FormInputID'], this.transition.condition[i]['@VersionStepID'], this.transition.key);

        this.transition.condition.splice(i, 1);
        this.transition.condition = this.transition.condition.slice();
        this.transitionChange.emit(this.transition);
    }

    edit(e: any) {
        this.condition = this.transition.condition;
        this.formMode = FormMode.Editing;
    }

    saveCondition(e: any) {
        if (this.formMode == FormMode.Adding) {
            if (e['@FormInputID'] != null)
                this.workflowFieldsService.pushUsedField(e['@FormInputID'], e['@VersionStepID'], this.transition.key, this.transition.name);

            this.transition.condition.push(e);
            this.transition.condition = this.transition.condition.slice();
            this.transitionChange.emit(this.transition);

        } else if (this.formMode == FormMode.Editing) {
            this.transition.condition = e;
        }

        this.formMode = FormMode.Default;
    }

    changeType(e: any) {
        this.transition.transitionType = e;
        this.transitionChange.emit(this.transition);
        this.filterFormFields();
    }

    filterFormFields() {
        this.formFields = this.workflowFieldsService.getFields();
        //console.log('filterFormFields: ', this.formFields, this.transition.formInputs);
        this.formFields = this.formFields.filter(f => this.transition.formInputs.indexOf(f['@stepId']) > -1);
    }
}