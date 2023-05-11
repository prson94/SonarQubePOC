import { Component, Input, OnInit } from '@angular/core';
import { TooltipInfo } from '../../../../models/tooltip-info.model';
import { ToolTipService } from '../../../../services/tooltip.service';
import { WorkflowMonitorItem } from '../../../../models/workflowmonitor.model';

@Component({
	selector: 'd3s-assignment-information-request',
	templateUrl: './assignment-information-request.component.html',
	styleUrls: ['./assignment-information-request.component.less']
})
export class AssignmentInformationRequestComponent implements OnInit {

	@Input() workflowAssignmentItem: WorkflowMonitorItem;

	isLoading: boolean;
	tooltipInfo: TooltipInfo;

	constructor(private toolTipService: ToolTipService) {
	}

	ngOnInit(): void {
	}

	loadData() {
		this.isLoading = true;
		this.toolTipService.getTooltipInfo('Issue', this.workflowAssignmentItem.Id)
			.subscribe((data) => {
				this.tooltipInfo = data;
				this.isLoading = false;
			});
	}

}
