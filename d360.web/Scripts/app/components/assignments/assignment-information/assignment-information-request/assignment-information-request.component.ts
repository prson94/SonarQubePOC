import { Component, Input, OnInit } from '@angular/core';
import { TooltipInfo } from '../../../../models/tooltip-info.model';
import { ToolTipService } from '../../../../services/tooltip.service';
import { AssignmentItem } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent implements OnInit {
	private _workflowItemUid: string;

	@Input() set workflowItemUid(value: string) {
		this._workflowItemUid = value;
		this.loadData();
	}

	@Input() assignmentItem: AssignmentItem;

	isLoading: boolean;
	tooltipInfo: TooltipInfo;

	constructor(private toolTipService: ToolTipService) {
	}

	ngOnInit(): void {
	}

	loadData() {
		this.isLoading = true;
		// this.toolTipService.getTooltipInfo('Issue', this._workflowItemUid.Id)
		// 	.subscribe((data) => {
		// 		this.tooltipInfo = data;
		// 		this.isLoading = false;
		// 	});
	}

}
