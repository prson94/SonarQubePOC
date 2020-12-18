import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Validators, ValidatorFn, AbstractControl, FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { SelectItem } from 'primeng/api';
import { AssetService } from '../../services/asset.service';


@Component({
    selector: 'gallery-property-group',
    templateUrl: './gallery.property-group.component.html',
    styles: [
        `.row {
            padding-bottom: 16px;
        }
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        .ul{
            padding: 16px;
            list-style-type: circle;
        }
        .gallery-form-container{
            max-width: 320px;
        }
        `
    ],
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryPropertyGroupComponent implements OnInit {
    properties: Array<any>;
    value: any;
    sampleUsage: string = `
<ig-property-group [form]="myForm">
    <div inputs>
        <-- form inputs here -->
    </div>
</ig-property-group>
`;
    testForm: FormGroup = null;
    multiTestForm: FormGroup = null;
    model: DummyformModel = new DummyformModel(null, null);
    defaultColors: SelectItem[] = [];
    formVal: any;
    formDateVal: any;
    constructor(private cdRef: ChangeDetectorRef, private fb: FormBuilder, private assetService: AssetService) {
      
    }

    ngOnInit(): void {
        this.testForm = this.fb.group({
            myFormName: ["bug", { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }],
            myFormAddr: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }],
            myFormEmail: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }]
        });
        this.multiTestForm = this.fb.group({
            myFormName: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }],
            myFormColor: [null, { validators: [Validators.required] }],
            myFormPetName: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }],
            myFormAddr: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }],
            myFormEmail: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }]
        });

        //must mark as dirty so validation message will appear for invalid example.
        this.testForm.get("myFormName").markAsDirty();
        this.testForm.updateValueAndValidity();

        this.assetService.getAllColors().subscribe(x => { this.defaultColors = x; });
        this.properties = new Array();
        this.properties.push({ Name: "igformGroup", Type: "FormGroup", Description: "The angular FormGroup object that contains the inputs.", Default: "" });
        this.properties.push({ Name: "title", Type: "string", Description: "The text to display at the top of the form group.", Default: "" });
        this.properties.push({ Name: "showMoreInfo", Type: "boolean", Description: "Turn on the option to display a help icon next to the title at the top of the form group.", Default: "" });
        this.properties.push({ Name: "moreInfoHtml", Type: "string", Description: "The HTML text to show in a tooltip when hovering over the help icon next to the title at the top of the form group. Used in conjunction with showMoreInfo.", Default: "" });
    }

    wordsIDontLike(wordsIDontLikeArr: string[]): ValidatorFn {
        type NewType = AbstractControl;

        return (control: NewType): { [key: string]: any } | null => {
            if (control.value == null)
                return {};
            if (control.value == null || wordsIDontLikeArr.map(x => { return x.toLowerCase() }).indexOf(control.value.toLowerCase()) != -1)
                return {
                    notNiceWord: { value: control.value }
                };
            return null;
        };
    }

    applyFocus(dd) {
        dd.applyFocus();
    }
    cars = [
        { label: 'Audi', value: 'Audi' },
        { label: 'BMW', value: 'BMW' },
        { label: 'Fiat', value: 'Fiat' },
        { label: 'Ford', value: 'Ford' },
        { label: 'Honda', value: 'Honda' },
        { label: 'Jaguar', value: 'Jaguar' },
        { label: 'Mercedes', value: 'Mercedes' },
        { label: 'Renault', value: 'Renault' },
        { label: 'VW', value: 'VW' },
        { label: 'Volvo', value: 'Volvo' }
    ];

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