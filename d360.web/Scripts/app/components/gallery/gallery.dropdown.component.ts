import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormGroup, FormBuilder, Validators, ValidatorFn, AbstractControl } from '@angular/forms';


@Component({
    selector: 'gallery-dropdown',
    templateUrl: './gallery.dropdown.component.html',
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

export class GalleryDropDownComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<input igInput type="text" name="name" />';
    disabledState: boolean = false;

    testForm: FormGroup = null;
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
    constructor(private fb: FormBuilder) { }

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "igSize", Type: "string", Description: "Size of the input. Options are small(150px), medium(308px), large(624px) and full(100%).", Default: "full" });
    }

}
