import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit } from '@angular/core';

@Component({
    selector: 'gallery-modal-drawer',
    templateUrl: './gallery.modal-drawer.component.html',
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

        label {
            color: black;
            font-weight: bold;
            display: block;
            margin-top: 12px;
        }
        `]
})

export class GalleryModalDrawerComponent implements OnInit {
    properties: Array<any>;
    isVisible: boolean = false;
    isModalVisible: boolean = false;
    isModalDrawer2Visible: boolean = false;
    radioSelection: string = null;

    sampleUsage = `
<ng-container header>
    Report Governance Issue
</ng-container>
<ng-container content>
    Modal Drawer test case with property groups
    <ig-property-group title="Form" [expanded]="true" [showHeaderLine]="false" [shouldBePadded]="false">
            ...
    </ig-property-group>
    <ig-property-group title="Collapse / Expand for content overflow" [expanded]="true" [showHeaderLine]="false" [shouldBePadded]="false">
            ...
    </ig-property-group>
</ng-container>
<ng-container action>
    <button class="button" (click)="isVisible=false">
        Cancel
    </button>
    <span class="grow"></span>
    <button class="button primary">
        Confirm
    </button>
</ng-container>`;


    constructor(private ref: ChangeDetectorRef) {
    }

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "isVisible", Type: "boolean", Description: "Is dialog visible?", Default: "false" });
        this.properties.push({ Name: "appendToBody", Type: "boolean", Description: "Attach the dialog to the body element", Default: "true" });
        this.properties.push({ Name: "additionalClasses", Type: "string", Description: "Additional styling classes", Default: "" });
        this.properties.push({ Name: "onClose", Type: "event", Description: "Event when drawer is close (escape button)", Default: "" });

    }
}
