import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';

export class ModalConfig {
    public title: string = "Default Title";
    public subtitle: string;
    public showTitle: boolean = true;
    public isVisible: boolean = false;
    public showConfirm: boolean = true;
    public additionalClasses: string = "small-dialog";
    constructor(title: string) {
        this.title = title;
    }
}

export class FiredEvent {
    public type: string;
    public event: string;
    constructor(t: string, e: string) {
        this.type = t;
        this.event = e;
    }
}

@Component({
    selector: 'gallery-modal',
    templateUrl: './gallery.modal.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`
        .gallery-section {
            padding: 0 16px 32px 16px;
        }
        .content ::ng-deep p {
            margin-top: 0px;
        }
        .eventoutput {
            display: inline-block;
            margin-top: 10px;
            padding: 10px;
            min-height: 1em;
            min-width: 200px;
            font-family: monospace;
        }
        .eventoutput .value {
            color: #6f7482;
            display: inline-block;
        }
        `]
})

export class GalleryModalComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    firedEvents: FiredEvent[] = [];
    modalBasic: ModalConfig = new ModalConfig("Basic Example");
    lorem: string = "<p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.</p>";
    loremRepeat: number = 3;
    sidePanelModalAdditionalClasses: string = "medium-dialog-with-side-panel";
    isModalVisible: boolean = false;

    sidePanelClassOptions = [
        { label: "medium-dialog", value: "medium-dialog-with-side-panel" },
        { label: "large-dialog", value: "large-dialog-with-side-panel" },
        { label: "xlarge-dialog", value: "xlarge-dialog" }
    ];

    classOptions = [
        { label: "small-dialog", value: "small-dialog" },
        { label: "medium-dialog", value: "medium-dialog" },
        { label: "large-dialog", value: "large-dialog" },
        { label: "xlarge-dialog", value: "xlarge-dialog" }
    ];

    constructor(private ref: ChangeDetectorRef) {
    }

    ngOnInit(): void {

        this.properties = new Array();
        this.properties.push({ Name: "title", Type: "string", Description: "Header title for dialog", Default: "Default Title" });
        this.properties.push({ Name: "subtitle", Type: "string", Description: "Subtitle for dialog", Default: "" });
        this.properties.push({ Name: "showTitle", Type: "boolean", Description: "Show dialog header with title/subtitle", Default: "true" });
        this.properties.push({ Name: "isVisible", Type: "boolean", Description: "Is dialog visible?", Default: "false" });
        this.properties.push({ Name: "showConfirm", Type: "boolean", Description: "Show standard Cancel/Confirm action buttons", Default: "false" });
        this.properties.push({ Name: "appendToBody", Type: "boolean", Description: "Attach the dialog to the body element", Default: "false" });
        this.properties.push({ Name: "additionalClasses", Type: "string", Description: "Additional styling classes", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "onConfirm", Description: "Fired when confirm action button is pressed. The event will also close the dialog, which will also fire the onClose event" });
        this.events.push({ Name: "onClose", Description: "Fired when the dialog closes" });

    }

    public registerEvent(type: string, $event: any) {
        const display = $event === null ? "null" : JSON.stringify($event);
        this.firedEvents.push(new FiredEvent(type, display));

        setTimeout(() => { this.firedEvents.shift(); this.ref.markForCheck(); }, 4000);
    }

    public getLorem(times: number = 1): string {
        return this.lorem.repeat(times);
    }

}
