import { Component, OnInit, ChangeDetectionStrategy, AfterContentInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { FormControl, Validators, FormGroup, ValidatorFn, AbstractControl, FormBuilder } from '@angular/forms';


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
    protected properties: Array<any>;
    protected sampleUsage: string = '<ig-number-input></ig-number-input>';
    private model: DummyformModel = new DummyformModel("name", 0);
    protected form: FormGroup = null;

    constructor(private fb: FormBuilder) { }

    ngOnInit(): void {
        this.form = this.fb.group({
            myNumber: [null, { validators: [Validators.required, this.numbersIdontLike([3, 5, 7])], updateOn: "blur" }]
        });
        this.properties = new Array();

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
}

export class DummyformModel {
    constructor(
        public name: string,
        public number: number
    ) { }
}
