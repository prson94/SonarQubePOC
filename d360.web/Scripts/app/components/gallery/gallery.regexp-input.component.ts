import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import 'codemirror/mode/javascript/javascript';
import 'codemirror/mode/markdown/markdown';

@Component({
    selector: 'gallery-regexp-input',
    templateUrl: './gallery.regexp-input.component.html',
    styles: [
        `
            .gallery-section {
                padding: 0 16px 32px 16px;            
            }

            .gallery-section h4 {
                padding-bottom: 8px;
            }
        `
    ], 
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryRegexpInputComponent {
    properties = [
        {
            Name: "disabled",
            Type: "boolean",
            Description: "Whether or not the regex input is disabled",
            Default: "false"
        },
        {
            Name: "required",
            Type: "boolean",
            Description: "Whether or not the regex input is required",
            Default: "false"
        },
        {
            Name: "showSamples",
            Type: "boolean",
            Description: "If true, it shows the kebab menu with menu selection to add the sample regex.",
            Default: "true"
        },
        {
            Name: "showValueValidator",
            Type: "boolean",
            Description: "If true, the second row to validate the provided regex with a test value, and a Validate button.",
            Default: "true"
        }
    ]
    events = [
    ];

    sampleUsage: string = '<ig-regexp-input [(ngModel)]="value" required></ig-regexp-input>';

    exampleStandard: string = ``;
    exampleRequired: string = ``;
    exampleNoSamples: string = ``;
    exampleNoValueValidator: string = ``;
    exampleNoSamplesAndNoValueValidator: string = ``;
    exampleDisabled: string = ``;
}
