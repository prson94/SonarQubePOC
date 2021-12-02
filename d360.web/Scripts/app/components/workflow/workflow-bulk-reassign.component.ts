import { Input, Output, Component, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { WorkflowService } from '../../services/workflow.service';
import { EventEmitter } from '@angular/core';
import { BulkWorkflowReassignModel } from '../../models/workflow.model';
import { ResourcesService } from '../../services/resources.service';
import { EditorField } from '../../models/editor-field.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflow-bulk-reassign',
    templateUrl: 'workflow-bulk-reassign.component.html',
    providers: [WorkflowService, ResourcesService]
})

export class WorkflowBulkReassignComponent extends BaseComponent implements OnInit, OnDestroy {
    @Output() onClose = new EventEmitter();
    @Output() onComplete = new EventEmitter();
    @Input() model: BulkWorkflowReassignModel = null;
    @Input() title: string = 'Form Reassignment';

    private items: any[] = [];
    private resource: any;
    field: EditorField;
    selectedResourceName: string = '';
    sendFormEmails: boolean = false;
    


    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected messagesService: MessagesObservableService,
        protected resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService,
        protected titleService: Title,
        protected workflowService: WorkflowService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.field = new EditorField();
        this.field.TypeaheadUri = `services/workflow/resources?excludedResourceId=${this.model.OriginalAssigneeResourceID}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = false;

        this.resourcesService.getResource(this.model.OriginalAssigneeResourceID)
            .subscribe(response => {
                this.items = response.items;
                if (this.items.length > 0) {
                    this.resource = this.items[0];
                }
                this.model.OriginalAssigneeResourceName = `${this.resource.FirstName} ${this.resource.LastName}`;
            });
    }

    ngOnDestroy() {

    }

    set fieldValue(value) {
        this.field.Value = value;
        if (this.field.Value != null && this.field.Value.length > 0) {
            this.model.NewAssigneeResourceID = +this.field.Value[0].split('|')[1]; 
            this.model.NewAssigneeResourceName = this.field.Value[0].split('|')[2];
        }
    }

    set dontSendFormEmails(value) {
        this.model.SendFormEmails = !value;
    }

    save() {
        this.isLoading = true;
        this.workflowService.postWorkflowBulkReassign(this.model)
            .subscribe(response => {
                this.isLoading = false;
                if (response.type != null && response.type == 'success') {
                    this.showMessageForResult(this.messagesService, response);
                    //console.log('submit complete', response);
                }
                this.onComplete.emit(response);
            });
    }

    valid() {
        let valid = true;

        if (this.field.Value == null || this.field.Value.length < 1)
            valid = false;

        return valid;
    }
}