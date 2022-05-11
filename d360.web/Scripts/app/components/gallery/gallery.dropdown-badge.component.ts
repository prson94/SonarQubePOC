import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';


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

        .status-circle {
            width: 8px;
            height: 8px;
            border-radius: 100%;
            display: inline-block;
            margin-right: 8px;
        }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryDropdownBadgeComponent implements OnInit {
    dropdownProperties: Array<any> = [];
    optionProperties: Array<any> = [];
    events: Array<any> = [];

    basicValue: number = 1;
    customValue: number = 1;
    statusValue: Status = Status.PENDING;
    statusEnum: typeof Status = Status;

    constructor(private ref: ChangeDetectorRef) { }

    ngOnInit(): void {
        this.dropdownProperties.push({ Name: "editable", Type: "boolean", Description: "Defines if user can interact with component", Default: "true" });
        this.dropdownProperties.push({ Name: "placeholder", Type: "string", Description: "Defines the text, which is being displayed when nothing is selected", Default: "Select an item..." });
        
        this.optionProperties.push({ Name: "value", Type: "any", Description: "Defines value, that will be emitted by component, when option is selected", Default: "" });
        this.optionProperties.push({ Name: "label", Type: "string", Description: "Defines the text representation of value, that has been selected, shown in the badge.", Default: "" });
        this.optionProperties.push({ Name: "custom", Type: "boolean", Description: "Defines whether content inside option or just label will be rendered", Default: "false" });
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