import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { CompanySettingsService } from '../../../../services/settings.service';
import { BaseComponent } from '../../../shared/base.component';

@Component({
    selector: 'd3s-workflow-condition-list',
    templateUrl: './workflow-condition-list.component.html'
})

export class WorkflowConditionListComponent extends BaseComponent implements OnChanges, OnInit {
    @Input() conditions: any[] = [];
    @Input() selection;
    @Input() readonly = false;
    @Input() satisfyAll: boolean = true;
    @Input() hideAllAnyOption: boolean = false;
    @Output() selectionChange = new EventEmitter();
    @Output() addClick = new EventEmitter();
    @Output() removeClick = new EventEmitter();
    @Output() editClick = new EventEmitter();
    @Output() connectorChange = new EventEmitter();

    filteredConditions: any[] = [];

    excludedContextualFields = [
        'IssueObject',
        'IssueObjectID',
        'ScoreType'
    ];

    ngOnInit() {
        this.satisfyAll = this.conditions.every((c) => c["@Connector"] === "AND");
    }

    ngOnChanges(changes: SimpleChanges) {
        this.filteredConditions = this.conditions.filter((c) => c['@ContextualFieldID'] == null || this.excludedContextualFields.indexOf(c['@ContextualFieldID']) === -1);
    }

    isAllAnyVisible() {
        return !this.hideAllAnyOption && this.conditions.filter((x) => x["@FieldTypeID"]).length > 1;
    }

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    operatorLabel(item: any): string {
        if (item == null || item['@Operator'] == null)
            {return null;}

        switch (item['@Operator']) {
            case 'C':
                return $localize`value changed`;
            case 'P':
                return $localize`is populated`;
            case 'NP':
                return $localize`is not populated`;
            default:
                return item['@Operator'];
        }
    }

    valueLabel(item: any): string {
        if (item == null || item['@Operator'] == null)
            {return null;}

        switch (item['@Operator']) {
            case 'C':
                return $localize`[any value change]`;
            case 'P':
                return $localize`[any value]`;
            case 'NP':
                return $localize`[no value]`;
            default:
                return (item['@ValueLabel'] == null ? item['@Value'] : item['@ValueLabel']);
        }
    }
}