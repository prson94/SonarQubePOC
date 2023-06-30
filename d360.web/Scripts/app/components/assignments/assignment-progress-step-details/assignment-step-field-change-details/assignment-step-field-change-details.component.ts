import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { WorkflowStepFieldChangeDetail } from '../../../../models/workflow.model';

/*global $localize*/

@Component({
	selector: 'd3s-assignment-step-field-change-details',
	templateUrl: './assignment-step-field-change-details.component.html'
})
export class AssignmentStepFieldChangeDetailsComponent extends BaseComponent implements OnInit {
	@Input() fieldChanges: any;

	constructor(protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	getHtmlFieldValue(item: WorkflowStepFieldChangeDetail) {
		if (typeof item.Value === 'undefined') {
			return '';
		}
		return item.Value;
	}

	getUrl(val: string): string {
		if (typeof val !== 'undefined') {
			const url: string[] = val.split('|');
			return url[1];
		}
		return '';
	}

	getName(val: string): string {
		if (typeof val !== 'undefined') {
			const name: string[] = val.split('|');
			return name[0];
		}
		return '';
	}

	getFieldName(item: WorkflowStepFieldChangeDetail): string {
		if (item.ObjectType !== '' && item.ObjectType !== 'Issue') {
			return $localize`Asset Field` + '::' + item.FieldName;
		}
		return $localize`Action Field` + '::' + item.FieldName;
	}

	ngOnInit(): void {
		if (this.fieldChanges) {
			for (const fieldChange of this.fieldChanges) {
				if (fieldChange.AppendValue === 'true') {
					fieldChange.ChangeType = 'Append';
				} else if (fieldChange.ClearValue === 'true') {
					fieldChange.ChangeType = 'Clear';
				} else {
					fieldChange.ChangeType = 'Replace';
				}
			}
		}
	}

}
