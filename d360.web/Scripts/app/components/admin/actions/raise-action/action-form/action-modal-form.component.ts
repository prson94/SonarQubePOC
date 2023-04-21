import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewEncapsulation } from "@angular/core";
import { Breadcrumb } from "../../../../../models/breadcrumb.model";
import { WorkflowIssueType } from "../../../../../models/workflow.model";

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
	@Input() breadCrumbs: Breadcrumb[] = [];
	@Output() onClose = new EventEmitter();
	@Output() onSave = new EventEmitter();
	@Input() hasSidePanel: boolean = false;

	editorDecription: string;
	selection: Record<string, object> = null;
	constructor(
		private cdRef: ChangeDetectorRef
	) {
	}

	ngOnChanges(): void {
		this.cdRef.markForCheck();
		this.editorDecription = this.issueType?.Description;
	}

	close() {
		this.onClose.emit();
	}

	save($event) {
		this.onSave.emit($event);
	}

}
