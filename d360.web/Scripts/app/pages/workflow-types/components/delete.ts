import { Component, EventEmitter, Input, Output } from '@angular/core';
import { WorkflowService } from '../../../services/workflow.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { BaseComponent } from '../../../components/shared/base.component';
import { CoreModule } from '../../../components/shared/core.module';

@Component({
    selector: 'workflow-type-delete',
	templateUrl: 'delete.html',
	standalone: true,
	imports: [CoreModule]
})
export class WorkflowTypeDelete extends BaseComponent {
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
