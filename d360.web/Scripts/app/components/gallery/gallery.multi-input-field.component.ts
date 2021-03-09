import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, ValidatorFn } from '@angular/forms';
import { debug } from 'util';


@Component({
    selector: 'gallery-multi-input-field',
    templateUrl: './gallery.multi-input-field.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }

        .requiredMessage{
            padding: 5px;
            color: #900;
            font-weight: bold;
        }
        `
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryMultiInputFieldComponent implements OnInit {
    properties: Array<any>;
    events: Array<any>;
    sampleUsage: string = '<ig-multi-input-field  [(ngModel)]="value" igSize="large"></ig-multi-input-field>';

    value;
    formValue;
    private multiValue: string[] = ["First Chip", "Earth"];
    private forTooltip: string[] = ["First Chip", "Earth", "Moon", "Sun", "Uranus", "Mars"];
    private multiValueInvalid: string[] = ["Duplicate", "NoN-Duplicate", "Duplicate"];

    constructor(private ref: ChangeDetectorRef) { }

    myForm = new FormGroup({
        multiInput: new FormControl(this.multiValueInvalid, [NoDuplicate()]),
    })

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "ngModel", Type: "string", Description: "Model representing the value of the multi input field ", Default: "null" });
        this.properties.push({ Name: "tabindex", Type: "number", Description: "Index of the element in tabbing order.", Default: "0" });
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Used to set the control to disabled state where the user cannot interact with it", Default: "false" });
        this.properties.push({ Name: "required", Type: "boolean", Description: "When this attribute is present the control must have a selected value to be valid", Default: "" });
        this.properties.push({ Name: "infoTooltip", Type: "string", Description: "When this attribute is present the control will show a tooltip with 'i' icon", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Description: "Fired when the selection changes" });

        //wait for icons to load
        setTimeout(() => this.ref.markForCheck(), 500);
    }
}
export function NoDuplicate(): ValidatorFn {

    return (control: AbstractControl): ValidationErrors | null => {
        let val: string[] = control.value;
        if (hasDuplicates(val)) {
            return { 'duplicates': true }
        }
        return null;

    }
    function hasDuplicates(array) {
        var valuesSoFar = Object.create(null);
        for (var i = 0; i < array.length; ++i) {
            var value = array[i];
            if (value in valuesSoFar) {
                return true;
            }
            valuesSoFar[value] = true;
        }
        return false;
    }

}
