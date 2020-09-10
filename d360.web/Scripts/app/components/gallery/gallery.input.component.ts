import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ValidatorFn, AbstractControl } from '@angular/forms';


@Component({
    selector: 'gallery-input',
    templateUrl: './gallery.input.component.html',
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

export class GalleryInputComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<input igInput type="text" name="name" />';
    protected disabledState: boolean = false;
   
    private model: DummyformModel = new DummyformModel("name", 0);
    protected testForm: FormGroup = null;

    constructor(private fb: FormBuilder) { }

    ngOnInit(): void {

        this.testForm = this.fb.group({
            myFormName: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }]
        });


        this.properties = new Array();
        this.properties.push({ Name: "igSize", Type: "string", Description: "Size of the input. Options are small(150px), medium(308px), large(624px) and full(100%).", Default: "full" });
    }

    wordsIDontLike(wordsIDontLikeArr: string[]): ValidatorFn {
        type NewType = AbstractControl;

        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null)
                return {};
            if (control.value == null || wordsIDontLikeArr.indexOf(control.value) != -1)
                return {
                    notNiceWord: { value: control.value }
                };
            return null;
        };
    }



    toggleDisabled() {
        this.disabledState = !this.disabledState;
    }
    submitTemplateForm(form) {
        console.log(form);
    }
    get diagnostic() {
        return JSON.stringify(this.model);
    }

}
export class DummyformModel {
    constructor(
        public theName: string,
        public number: number
    ) { }
}