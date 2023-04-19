import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewEncapsulation } from "@angular/core";
import { WorkflowIssueType } from "../../../../../models/workflow.model";
/*global $localize*/

@Component({
	selector: "action-modal-form",
	templateUrl: './action-modal-form.component.html',
	styleUrls: [`action-modal-form.component.less`],
	encapsulation: ViewEncapsulation.None,
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ActionModalFormComponent implements OnChanges {
	@Input() isModalVisible: boolean = false;
	@Input() issueType: WorkflowIssueType;

	@Output() onClose = new EventEmitter();
	@Output() onSave = new EventEmitter();

	selection: any = null;
	constructor(
		private cdRef: ChangeDetectorRef
	) {
	}

	ngOnChanges(changes: SimpleChanges): void {
		this.cdRef.markForCheck();
	}

	close() {
		this.onClose.emit();
	}

	save($event) {
		this.onSave.emit($event);
	}

}
