import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormGroup, FormControl, ValidatorFn, AbstractControl, Validators } from '@angular/forms';


@Component({
    selector: 'gallery-date-picker',
    templateUrl: './gallery.date-picker.component.html',
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

export class GalleryDatePickerComponent implements OnInit {
    protected properties: Array<any>;
    protected events: Array<any>;
    protected sampleUsage: string = '<ig-date></ig-date>';
    protected form: FormGroup = null;
    protected val: any;

    ngOnInit(): void {
        this.form = new FormGroup({}, []);
        this.form.addControl('myDate', new FormControl(null, [Validators.required, this.startDateValidator(new Date())]));

        this.properties = new Array();
        this.properties.push({ Name: "disabled", Type: "boolean", Description: "Whether or not the textarea control is disabled", Default: "" });
        this.properties.push({ Name: "required", Type: "Boolean", Description: "Whether or not the textarea control is required", Default: "" });
        this.properties.push({ Name: "ngModel", Type: "Date", Description: "Model binding for the selected date object", Default: "" });
        this.properties.push({ Name: "style", Type: "string", Description: "Inline style of the component", Default: "" });
        this.properties.push({ Name: "styleClass", Type: "string", Description: "Style class of the component", Default: "" });
        this.properties.push({ Name: "inputStyle", Type: "string", Description: "Inline style of the input field", Default: "" });
        this.properties.push({ Name: "inputStyleClass", Type: "string", Description: "Style class of the input field", Default: "" });
        this.properties.push({ Name: "placeholder", Type: "string", Description: "Placeholder text string for the input control.", Default: "'Optional' or 'Value required' if required = true" });
        this.properties.push({ Name: "appendTo", Type: "any", Description: "Target element to attach the overlay, valid values are 'body' or a local ng-template variable of another element", Default: "" });
        this.properties.push({ Name: "maxDate", Type: "Date", Description: "The minimum selectable date", Default: "" });
        this.properties.push({ Name: "minDate", Type: "Date", Description: "The maximum selectable date", Default: "" });
        this.properties.push({ Name: "dateFormat", Type: "string", Description: "The format of the selected date string", Default: "mm/dd/yy" });
        this.properties.push({ Name: "name", Type: "string", Description: "Name of the input element or form control", Default: "" });
        this.properties.push({ Name: "form", Type: "FormGroup", Description: "The FormGroup this control is a part of when using Reactive Forms", Default: "" });

        this.events = new Array();
        this.events.push({ Name: "ngModelChange", Type: "", Description: "", Default: "" });
    }

    startDateValidator(startDate: Date): ValidatorFn {
    return (control: AbstractControl): { [key: string]: any } | null => {
        if (control.value == null)
            return { };
        if (control.value == null || control.value < startDate)
            return {
                invalidDate: { value: control.value }
            };
        return null;
    };
}
}
