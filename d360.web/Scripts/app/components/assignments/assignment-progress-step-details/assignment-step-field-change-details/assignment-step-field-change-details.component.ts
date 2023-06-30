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
			let url: string[] = val.split('|');
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
			for (let i = 0; i < this.fieldChanges.length; i++) {
				if (this.fieldChanges[i].AppendValue === 'true') {
					this.fieldChanges[i].ChangeType = 'Append';
				} else if (this.fieldChanges[i].ClearValue === 'true') {
					this.fieldChanges[i].ChangeType = 'Clear';
				} else {
					this.fieldChanges[i].ChangeType = 'Replace';
				}
			}
		}
	}

}
