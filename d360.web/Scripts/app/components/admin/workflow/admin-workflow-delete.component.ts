import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';

import * as _ from 'lodash';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-workflow-delete',
    providers: [WorkflowService],
    template: `
<div class="row">
    <div class="col s12" i18n>
        Are you sure you want to delete this workflow?
    </div>
</div>
<div class="row" style="padding-top:10px;">
    <div class="col s12">
        <button pButton i18n-label label="Delete" (click)="delete()" [disabled]="isLoading"></button>
        <button pButton i18n-label label="Cancel" (click)="onCancel.emit()"></button>
    </div>
</div>
`
})

export class AdminWorkflowDeleteComponent extends BaseComponent implements OnInit {
    @Input() id: number=0;
    @Input() uid: string = "00000000-0000-0000-0000-000000000000";
    @Output() onCancel = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onComplete = new EventEmitter();


    constructor(
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService) {
        super(settingsService);
    }

    ngOnInit() {

    }

    delete() {
        this.isLoading = true;
        this.workflowService.deleteWorkflowType(this.id,this.uid)
            .subscribe(r => {
                this.onSuccess.emit();
                this.onComplete.emit();
                this.isLoading = false;
            }, err => {
                this.onComplete.emit();
                this.isLoading = false;
                return err;
            });

    }

}
