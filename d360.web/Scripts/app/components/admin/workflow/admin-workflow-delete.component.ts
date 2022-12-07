import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { WorkflowService } from '../../../services/workflow.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-workflow-delete',
    providers: [WorkflowService],
    templateUrl: 'admin-workflow-delete.component.html'
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
            .subscribe((r) => {
                this.onSuccess.emit();
                this.onComplete.emit();
                this.isLoading = false;
            }, (err) => {
                this.onComplete.emit();
                this.isLoading = false;
                return err;
            });

    }

}
