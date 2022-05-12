import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { DropdownBadgeOption } from '../shared/controls/dropdown-badge/types/dropdown-bage-option.type';


@Component({
    selector: 'gallery-dropdown-badge',
    templateUrl: './gallery.dropdown-badge.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }

        pre {
            margin: 0;
        }

        .component-container {
            width: 200px;
        }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryDropdownBadgeComponent implements OnInit {
    dropdownProperties: Array<any> = [];
    optionProperties: Array<any> = [];
    events: Array<any> = [];

    numberOptions: DropdownBadgeOption<number>[];
    statusOptions: DropdownBadgeOption<StatusOption>[];

    basicValue: number = 1;
    customValue: number = 1;
    statusValue: StatusOption;

    constructor() { }

    ngOnInit(): void {
        this.numberOptions = [{label: '1', value: 1}, {label: '2', value: 2}, {label: '3', value: 3}];
        this.statusOptions = [
            {label: 'Pending', value: {status: Status.PENDING, color: '#ffcc00'}},
            {label: 'In Progress', value: {status: Status.IN_PROGRESS, color: '#049649'}},
            {label: 'On Hold', value: {status: Status.ON_HOLD, color: '#962470'}},
            {label: 'Blocked', value: {status: Status.BLOCKED, color: '#d11947'}}
        ];
        this.statusValue = this.statusOptions[0].value;

        this.dropdownProperties.push({ Name: "disabled", Type: "boolean", Description: "Defines if user can interact with component", Default: "false" });
        this.dropdownProperties.push({ Name: "required", Type: "boolean", Description: "Defines if value should be selected to pass validation", Default: "" });
        this.dropdownProperties.push({ Name: "placeholder", Type: "string", Description: "Defines the text, which is being displayed when nothing is selected", Default: "Select an item..." });
        this.dropdownProperties.push({ Name: "options", Type: "DropdownBadgeOption[]", Description: "Defines the list of options, that can be chosen in dropdown", Default: "[]" });

        this.optionProperties.push({ Name: "value", Type: "any", Description: "Defines value, that will be emitted by component, when option is selected", Default: "" });
        this.optionProperties.push({ Name: "label", Type: "string", Description: "Defines the text representation of value, that has been selected, shown in the badge.", Default: "" });
        this.optionProperties.push({ Name: "disabled", Type: "boolean", Description: "Defines if this option can be selected", Default: "false" });

        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });

    }

}

enum Status {
    PENDING = "PENDING",
    ON_HOLD = "ON HOLD",
    IN_PROGRESS = "IN PROGRESS",
    BLOCKED = "BLOCKED"
}

type StatusOption = {status: Status; color: string};