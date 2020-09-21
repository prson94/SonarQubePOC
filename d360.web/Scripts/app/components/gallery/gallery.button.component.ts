import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-button',
    templateUrl: './gallery.button.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ],    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryButtonComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<button igButton icon="fa-ellipsis-v" tooltip="More..."></button>';
    loadingState: boolean = false;
    disabledState: boolean = false;
    clicks: string[] = [];

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "label", Type: "string", Description: "Text of the button. Buttons without a label must always provide a tooltip.", Default: "" });
        this.properties.push({ Name: "icon", Type: "string", Description: "Name of the icon.", Default: "" });
        this.properties.push({ Name: "tooltip", Type: "string", Description: "Tooltip for button. Must be provided if there is no label. Will also be used as ARIA label.", Default: "" });
        this.properties.push({ Name: "loading", Type: "boolean", Description: "When present, it specifies that the component should be in loading state. When loading, the button is also disabled.", Default: "false" });
    }

    toggleDisabled() {
        this.disabledState = !this.disabledState;
    }

    toggleLoading() {
        //Removing loading state enables the button, so we'll update the disabledState flag to match
        if (this.loadingState && this.disabledState)
            this.disabledState = false;

        this.loadingState = !this.loadingState;
    }
}
