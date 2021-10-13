import { Component, OnInit, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { Resource } from '../../../../models/resource.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { ResourcesService } from '../../../../services/resources.service';
import { map } from 'rxjs/operators';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-form-history',
    providers: [WorkflowService, ResourcesService],
    templateUrl: './workflow-form-history.component.html'
})

export class WorkflowFormHistoryComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() itemStepId: number;
    @Input() settings: any;
    @Input() fields: any;
    @Input() completed: boolean = false;

    resources: Resource[] = [];
    selectedFormIndex: number;
    selectedForm: any;

    constructor(
        private workflowService: WorkflowService,
        private resourcesService: ResourcesService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);

    }

    ngOnInit() {
        console.log(this.itemStepId, this.settings, this.fields);

        this.isLoading = true;
        this.resourcesService.getResources()
            .pipe(
                map(r => {
                this.resources = r; }),
            map(() => {
                //normalize input
                if (this.fields != null && this.fields.form != null) {
                    if (this.fields.form.constructor !== Array) {
                        let f = this.fields.form;
                        this.fields.form = [];
                        this.fields.form.push(f);
                    }

                    this.fields.form.forEach(f => {
                        if (f['@ResourceID'] != null) {
                            let r = this.resources.find(r => r.ID == +f['@ResourceID']);
                            f.ResourceName = r ? r.FirstName + ' ' + r.LastName : '[unknown]';
                        }

                        if (f.field != null) {
                            if (f.field.constructor !== Array) {
                                let l = f.field;
                                f.field = [];
                                f.field.push(l);
                            }
                        }
                    });
                }
            }),
            map(() => {
                //show the first completed form by default
                if (this.fields != null && this.fields.form != null && this.fields.form.length > 0) {
                    this.selectedFormIndex = 0;
                    this.selectedForm = this.fields.form[0];
                }
            }),
            map(() => this.isLoading = false)).subscribe();
    }

    ngOnChanges() {
        this.load();
    }

    load() {
    }

    selectForm(i: number) {
        if (i < 0) {
            this.selectedForm = null;
            return;
        }
        this.selectedForm = this.fields.form[i];
    }
}