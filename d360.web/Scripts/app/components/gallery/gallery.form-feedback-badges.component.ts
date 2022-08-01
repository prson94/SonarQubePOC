import { Component, ChangeDetectionStrategy, ViewChild, ElementRef } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';


@Component({
    selector: 'gallery-form-feedback-badges',
    templateUrl: './gallery.form-feedback-badges.component.html',
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
export class GalleryFormFeedbackBadgesComponent {
    formGroup = new FormGroup({
        name: new FormControl('', [Validators.required, Validators.minLength(2), Validators.maxLength(10)]),
        count: new FormControl(null, [Validators.required, Validators.min(2), Validators.max(10)]),
        simpleField: new FormControl('', [])
    });

    @ViewChild('form', { static: false }) formElement: ElementRef;

    properties = [
        { Name: "igformGroup", Type: "FormGroup", Description: "Angular FormGroup", Default: "" },
        { Name: "inputContainer", Type: "ElementRef", Description: "<form> element", Default: "" }
    ];

    sampleUsage: string = `
<ig-form-feedback-badges 
    [igformGroup]="formGroup"
    [inputContainer]="formElement">
</ig-form-feedback-badges>
    `;

    configuringUsage: string = `
// configuring form to be ready for passing into form-feedback-badges (component.html)
<form #form [formGroup]="formGroup">
    …
</form>

// configuring form to be ready for passing into form-feedback-badges (component.ts)
@ViewChild('form', { static: false }) formElement: ElementRef;
`;
}
