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
        `
    ],
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryPropertyGroupComponent implements OnInit {
    properties: Array<any>;
    value: any;
    sampleUsage: string = `
<ig-propert-group [form]="myForm">
    <div inputs>
        <-- form inputs here -->
    </div>
</ig-propert-group>
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
            myFormName: [null, { validators: [Validators.required, this.wordsIDontLike(["No", "Defect", "Bug"])], updateOn: "blur" }],
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

        this.assetService.getAllColors().subscribe(x => { this.defaultColors = x; });
        this.properties = new Array();
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
    get diagnostic() {
        return JSON.stringify(this.model);
    }

    checkFormValue(value, control: FormControl) {
        console.log(control);
    }
}
export class DummyformModel {
    constructor(
        public theName: string,
        public number: number
    ) { }
}