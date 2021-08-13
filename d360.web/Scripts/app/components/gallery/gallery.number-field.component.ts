import { Component, OnInit, ChangeDetectionStrategy, AfterContentInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { FormControl, Validators, FormGroup, ValidatorFn, AbstractControl, FormBuilder } from '@angular/forms';

export class DummyformModel {
    constructor(
        public name: string,
        public number: number
    ) { }
}

export class DummyenforceModel {
    constructor(
        public name: string,
        public number: number,
        public enforce: boolean
    ) { }
}

@Component({
    selector: 'gallery-number-field',
    templateUrl: './gallery.number-field.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ]
})

export class GalleryNumberFieldComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<ig-number-input></ig-number-input>';
    model: DummyformModel = new DummyformModel("name", 0);
    enforcemodel: DummyenforceModel = new DummyenforceModel("enforcemodel", 0, true);
    form: FormGroup = null;
    demoSize: string = "small";

    constructor(private fb: FormBuilder) { }

    ngOnInit(): void {
        this.form = this.fb.group({
            myNumber: [null, { validators: [Validators.required, this.numbersIdontLike([3, 5, 7]), Validators.min(1), Validators.max(10)], updateOn: "blur" }]
        });


        this.properties = new Array();
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Whether or not the number field control is disabled", Default: "" });
        this.properties.push({ Name: "required", Type: "Boolean", Description: "Whether or not the number field control is required", Default: "" });
        this.properties.push({ Name: "ngModel", Type: "Date", Description: "Model binding for the selected number field object", Default: "" });
        this.properties.push({ Name: "styleClass", Type: "string", Description: "Style class of the component", Default: "" });
        this.properties.push({ Name: "placeholder", Type: "string", Description: "Placeholder text string for the input control.", Default: "'Optional' or 'Value required' if required = true" });
        this.properties.push({ Name: "max", Type: "Date", Description: "The minimum number allowed", Default: "" });
        this.properties.push({ Name: "min", Type: "Date", Description: "The maximum number allowed", Default: "" });
        this.properties.push({ Name: "enforceMaxMin", Type: "Boolean", Description: "Whether or not to enforce min/max and provide 'underMin' and 'overMax' validation errors.", Default: "" });
        this.properties.push({ Name: "step", Type: "string", Description: "The amount to increment/decrement the value by", Default: "mm/dd/yy" });
        this.properties.push({ Name: "name", Type: "string", Description: "Name of the input element or form control", Default: "" });
        this.properties.push({ Name: "igSize", Type: "string", Description: "Size of the input. Options are small(150px), medium(308px), large(624px) and full(100%).", Default: "small" });
        this.properties.push({ Name: "tabindex", Type: "number", Description: "Index of the element in tabbing order.", Default: "0" });
        this.properties.push({ Name: "ariaLabel", Type: "string", Description: "Aria-label attribute is used to define a string that labels the current element.", Default: "" });
        this.properties.push({ Name: "ariaRequired", Type: "string", Description: "The aria-required attribute is used to indicate that user input is required on an element before a form can be submitted", Default: "" });
        this.properties.push({ Name: "ariaInvalid", Type: "string", Description: "The aria-invalid attribute is used to indicate that the value entered into an input field does not conform to the format expected by the application.", Default: "" });
    }
    numbersIdontLike(numberIDontLike: number[]): ValidatorFn {
        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control.value == null)
                return {};
            if (control.value == null || numberIDontLike.indexOf(parseFloat(control.value)) != -1)
                return {
                    notNiceNumber: { value: control.value }
                };
            return null;
        };
    }

    submitTemplateForm(form) {
        console.log(form);
    }
    get diagnostic() { return JSON.stringify(this.model); }
    get enforcediagnostic() { return JSON.stringify(this.enforcemodel); }
    get JSONERR() { return JSON.stringify(this.form.get('myNumber').errors);}
}

