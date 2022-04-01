import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import 'codemirror/mode/javascript/javascript';
import 'codemirror/mode/markdown/markdown';

@Component({
    selector: 'gallery-codearea',
    templateUrl: './gallery.codearea.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;            
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }        
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryCodeAreaComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    sampleUsage: string = '<codearea [(ngModel)]="value"></codearea>';

    exampleCode: string = `
    {
	    "qualifier": "PII.SSN",
	    "headerRegExps": [ ".*(?i)(SSN|Social).*" ],
	    "headerRegExpConfidence": [70, 100],
	    "regExpReturned": "\\\\d{3}-\\\\d{2}-\\\\d{4}",
	    "threshold": 98,
	    "priority": 112,
	    "baseType": "STRING"
    }
    `;

    exampleCodeCss: string = `body {
     padding: 0;
     margin: 0;
     background: #f7f7f7;
}

/** Header styles **/
.header {
     background-image: url('https://path-to-image.jpg');
     height: 400px;
     background-position: center center;
}

.main p {
     color: black;
     text-align: center;
     line-height: 24px;
     font-family: 'Raleway', sans-serif;
     font-size: 16px;
     font-weight: 400;
}`;

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Whether or not the codearea control is disabled", Default: "false" });
        this.properties.push({ Name: "required", Type: "boolean", Description: "Whether or not the codearea control is required", Default: "false" });
        this.properties.push({ Name: "readonly", Type: "boolean", Description: "Whether or not the codearea control is readonly", Default: "false" });
        this.properties.push({ Name: "placeholder", Type: "string", Description: "Placeholder text for empty fields", Default: "Optional" });
        this.properties.push({ Name: "codeType", Type: "string", Description: "Type of code the field is populated with. Allowed values are json and css", Default: "json" });
        this.properties.push({ Name: "igSize", Type: "string", Description: "Size of the input. Options are large(624px) and full(100%).", Default: "full" });
        
        this.events = new Array();
        this.events.push({ Name: "isValid", Type: "boolean", Description: "Outputs the current validation state of the field when changed.", Default: "" });
    }
}
