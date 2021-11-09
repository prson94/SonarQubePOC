import { Component, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    LinkModel,
    TransitionType,
    TransitionTypeInfo,
    WorkflowChangeType,
} from '../../../../models/workflow.model';
import { FormMode } from '../../../../models/form.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';

import * as _ from 'lodash';
import * as go from 'gojs';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-transition-editor',
    providers: [WorkflowService],
    templateUrl: './workflow-transition-editor.component.html'
})

export class WorkflowTransitionEditorComponent extends BaseComponent implements OnInit, OnDestroy, OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() issueObject: string;
    @Input() transition: LinkModel;
    @Input() diagram: go.Diagram;
    @Input() workflowChangeType: WorkflowChangeType;
    @Output() transitionChange = new EventEmitter();

    private originalTransition: LinkModel;
    transitionTypes: TransitionTypeInfo[] = [];
    private condition = null;

    TransitionType = TransitionType;

    private fieldsSub: any;
    private httpFieldsSub: any;
    private outputFieldsSub: any;
    private formFields: any[] = [];
    private httpFields: any[] = [];
    private outputFields: any[] = [];
    private formMode = FormMode.Default;

    FormMode = FormMode;


    constructor(
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService) {
        super(settingsService);
    }

    ngOnInit() {
        this.originalTransition = _.cloneDeep(this.transition);
        this.workflowService.getTransitionTypes()
            .subscribe(r => {
                this.transitionTypes = r;
            });

        this.filterFormFields();
        this.filterHttpFields();
        this.filterOutputFields();

        this.fieldsSub = this.workflowFieldsService.formFields$.subscribe(() => {
            this.filterFormFields();
        });

        this.httpFieldsSub = this.workflowFieldsService.httpFields$.subscribe(() => {
            this.filterHttpFields();
        });

        this.outputFieldsSub = this.workflowFieldsService.outputFields$.subscribe(() => {
            this.filterOutputFields();
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        if (!changes['transition'].isFirstChange() && changes['transition'].currentValue.key != changes['transition'].previousValue.key) {
            this.formMode = FormMode.Default;
            this.filterFormFields();
            this.filterHttpFields();
            this.filterOutputFields();
        } else if (!changes['transition'].isFirstChange()) {
            this.filterFormFields();
            this.filterHttpFields();
            this.filterOutputFields();
        }
    }

    ngOnDestroy() {
        if (this.fieldsSub) {
            this.fieldsSub.unsubscribe();
        }
        if (this.httpFieldsSub) {
            this.httpFieldsSub.unsubscribe();
        }
        if (this.outputFieldsSub) {
            this.outputFieldsSub.unsubscribe();
        }
    }

    add() {
        this.condition = null;
        this.filterFormFields();
        this.filterHttpFields();
        this.filterOutputFields();

        this.formMode = FormMode.Adding;
    }

    remove(e: any) {
        let i = this.transition.condition.findIndex(c => c == e);

        if (e["@FormInputID"] != null) {
            this.workflowFieldsService.deleteUsedField(this.transition.condition[i]["@FormInputID"], this.transition.condition[i]["@VersionStepID"], this.transition.key);
        }

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
            if (e["@FormInputID"] != null) {
                this.workflowFieldsService.pushUsedField(e["@FormInputID"], e["@VersionStepID"], this.transition.key, this.transition.name);
            }

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
        this.filterHttpFields();
        this.filterOutputFields();
    }

    filterFormFields() {
        this.formFields = this.workflowFieldsService.getFields();
        this.formFields = this.formFields.filter((f) => this.transition.formInputs.indexOf(f["@stepId"]) > -1);
    }

    filterHttpFields() {
        this.httpFields = this.workflowFieldsService.getHttpFields();
        this.httpFields = this.httpFields.filter((f) => this.transition.httpInputs.indexOf(f["@stepId"]) > -1);
    }

    filterOutputFields() {
        this.outputFields = this.workflowFieldsService.getOutputFields();
        this.outputFields = this.outputFields.filter((f) => this.transition.httpResponseInputs.findIndex((r) => r === f.StepId) > -1);
    }
}