import { Component, Input, OnInit } from '@angular/core';
import { TooltipInfo } from '../../../../models/tooltip-info.model';
import { ToolTipService } from '../../../../services/tooltip.service';
import { WorkflowAssignmentItem } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent implements OnInit {
	private _workflowAssignmentItem: WorkflowAssignmentItem;

	@Input() set workflowAssignmentItem(value: WorkflowAssignmentItem) {
		this._workflowAssignmentItem = value;
		this.loadData();
	}

	isLoading: boolean;
	tooltipInfo: TooltipInfo;

	constructor(private toolTipService: ToolTipService) {
	}

	ngOnInit(): void {
	}

	loadData() {
		this.isLoading = true;
		// this.toolTipService.getTooltipInfo('Issue', this._workflowAssignmentItem.Id)
		// 	.subscribe((data) => {
		// 		this.tooltipInfo = data;
		// 		this.isLoading = false;
		// 	});
	}

}
