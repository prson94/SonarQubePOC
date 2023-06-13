import {Component, Input} from '@angular/core';
import {WorkflowDiagramModel} from "../../../../../models/workflow.model";

@Component({
  selector: 'd3s-workflow-pending-assignments',
  templateUrl: './workflow-pending-assignments.component.html',
  styleUrls: ['./workflow-pending-assignments.component.less']
})
export class WorkflowPendingAssignmentsComponent {
	@Input() workflowDiagramModel: WorkflowDiagramModel

}
