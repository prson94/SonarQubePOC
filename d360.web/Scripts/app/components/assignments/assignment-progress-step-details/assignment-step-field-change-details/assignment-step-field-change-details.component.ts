import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { WorkflowStepFieldChangeDetail } from '../../../../models/workflow.model';

@Component({
  selector: 'd3s-assignment-step-field-change-details',
  templateUrl: './assignment-step-field-change-details.component.html',
  styleUrls: ['./assignment-step-field-change-details.component.less']
})
export class AssignmentStepFieldChangeDetailsComponent extends BaseComponent {
	@Input() fieldChanges: any;

	constructor(protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	getHtmlFieldValue(item: any) {
		if (typeof item.Value === 'undefined')
		{return '';}
		return item.Value;
	}

	getUrl(val: string): string {
		if (typeof val !== "undefined") {
			let url: string[] = val.split("|");
			return url[1];
		}
		return "";
	}

	getName(val: string): string {
		if (typeof val !== "undefined") {
			let name: string[] = val.split("|");
			return name[0];
		}
		return "";
	}

	getFieldName(item: WorkflowStepFieldChangeDetail): string {
		if (item.ObjectType !== '' && item.ObjectType !== 'Issue')
		{return $localize`Asset Field` + '::' + item.FieldName;}
		return $localize`Action Field` + '::' + item.FieldName;
	}

}
