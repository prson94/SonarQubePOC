import { Component, ChangeDetectionStrategy, Input, OnChanges, ChangeDetectorRef, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    NodeModel,
    WorkflowActivityType,
    StepType
} from '../../../../models/workflow.model';


import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';
import { WorkflowService } from '../../../../services/workflow.service';
import { GroupService } from '../../../../services/group.service';

import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { CompanySettingsService } from '../../../../services/settings.service';


@Component({
    selector: 'd3s-workflow-step-summary',
    templateUrl: './workflow-step-summary.component.html',
    providers: [ResponsibilityTypeService, WorkflowService, GroupService ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class WorkflowStepSummaryComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() object: string;
    @Input() objectId: number;
    @Input() step: NodeModel;
    @Input() issueObject: string;
    @Input() showSensitiveInfo = true;

    WorkflowActivityType = WorkflowActivityType;
    StepType = StepType;
    private states = [
        //'Unknown',
        { value: '0', label: 'Pending Add' },
        { value: '1', label: 'Active' },
        { value: '2', label: 'Pending Delete' },
        { value: '3', label: 'Deleted' },
    ];

    private responsibilities = [];
    private fields = [];

    private lookups = [];
    private groups: SelectItem[] = [];
    private intersectTypes = [];

    constructor(
        private groupService: GroupService,
        private responsibilityService: ResponsibilityTypeService,
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnInit() {
        this.isLoading = true;
        this.responsibilityService.getResponsibilityTypes()
            .subscribe(r => {
                this.responsibilities = r;
                this.workflowService.getWorkflowFieldTypes(this.objectId, this.object, true, this.issueObject)
                    .subscribe(f => {
                        this.fields = f;
                        this.load();
                    })
            })
    }
    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = false;

        if (this.step != null && this.step.settings != null) {
            if (this.step.activityType == WorkflowActivityType.EmailNotification || this.step.activityType == WorkflowActivityType.Form) {
                if (this.step.settings['MessageRecipientType'] == 'Responsibility') {
                    if (this.step.settings.ResponsibilityTypeID != null) {
                        if (!_.isArray(this.step.settings.ResponsibilityTypeID)) {
                            let id = this.step.settings.ResponsibilityTypeID
                            delete this.step.settings.ResponsibilityTypeID;
                            this.step.settings.ResponsibilityTypeID = [];
                            this.step.settings.ResponsibilityTypeID.push(id);
                        }
                    }
                } else if (this.step.settings['MessageRecipientType'] == 'Group') {
                    this.groupService.getGroups().subscribe(GroupList => {
                        this.groups = GroupList.items.map(g => { return { value: g.Uid, label: g.Name } });
                        if (this.step.settings.MessageToGroup != undefined) {
                            if (!this.groups.find(g => g.value == this.step.settings.MessageToGroup)) {
                                this.groups.push(<SelectItem>{ value: this.step.settings.MessageToGroup, label: '<invalid group>' });
                            }
                        }
                    });
                }
                if (this.step.activityType == WorkflowActivityType.Form) {
                    this.getLookups();
                }
            }
        }
        this.isLoading = false
        this.ref.markForCheck();
    }

    renderFieldChangeTableName(item: any): string {
        if (this.issueObject == "") return item['@FieldName'];
        if (typeof item['@ObjectType'] == 'undefined' || item['@ObjectType'] == 'Issue')
            return "Action Field::" + item['@FieldName'];
        else {
            let f = this.fields.find(f => f.ID == +item['@FieldId']);
            if (f == undefined)
                return "";
            return "Asset Field::" + f.FriendlyName;
        }
    }

    getResponsibilityName(i: number): string {
        let id = this.step.settings.ResponsibilityTypeID[i];
        if (id == null || +id < 0)
            return "";

        let r = this.responsibilities.find(r => r.ID == +id);

        if (r != null)
            return r.Name;
        return "";
    }

    getGroupName(): string {
        return (this.step.settings.MessageToGroup == null) ? '<none>' : this.groups.find(g => g.value == this.step.settings.MessageToGroup).label;
    }

    getLookups() {
        this.workflowService.getAllowIntersectTypes(this.object, this.objectId)
            .subscribe(r => {
                this.intersectTypes = r;
            });

        this.workflowService.getWorkflowVersionStepFormLookups(this.object, this.objectId)
            .subscribe(r => {
                this.lookups = r;
            });
    }

    isHtml(i: any): boolean {
        //console.log('isHtml', i, this.fields);
        if (i == null) return false;
        let f = this.fields.find(f => f.ID == +i['@FieldId']);
        if (f == null) return false;
        return f.Type == 'Html';
    }


    getValue(i: any): string {
        let val = "";
        if (i != null) {
            if (i['@ValueLabel'] != null)
                val = i['@ValueLabel'];
            else
                val = i['@Value'];
        }

        if (val == undefined || val == null)
            return '';

        if (val.length > 50) {
            val = val.substr(0, 47) + '...';
        }

        return val;
    }

    private getTypeLabel(i: any) {
        switch (i['@type']) {
            case 'list':
                if (this.lookups == null)
                    return 'List';
                let list = this.lookups.find(l => l.value.toString() == i['@referenceFieldId']);
                return 'List' + (list == null ? '' : ' :: ' + list.label);
            case 'relationshipType':
                if (this.intersectTypes == null)
                    return 'Relationship';
                let rel = this.intersectTypes.find(l => l.IntersectTypeID.toString() == i['@intersectTypeId']);
                return 'Relationship' + (rel == null ? '' : (' :: ' + ((rel.PredicateName != null && rel.PredicateName.length > 0) ? `[${rel.PredicateName}] ` : ' ') + rel.TargetName));
            default:
                return (i['@type'].charAt(0).toUpperCase() + i['@type'].substr(1));
        }
    }

}