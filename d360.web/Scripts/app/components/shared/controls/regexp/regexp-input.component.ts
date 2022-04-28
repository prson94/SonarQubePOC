import { Component, NgModule, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, EventEmitter, Output, ViewChild, OnInit } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR, NG_VALIDATORS, Validator, AbstractControl, ValidationErrors } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { PopupMenuModule } from "../popup-menu/popup-menu.component";
import { TooltipModule } from "primeng/tooltip";
import { DirectivesModule } from "../../../../directives/directives.module";
import { RegexpTesterComponent } from "./regexp-tester.component";

export const REGEXP_EDITOR_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => RegexpInputComponent),
    multi: true
};
export const REGEXP_INPUT_VALIDATOR: any = {
    provide: NG_VALIDATORS,
    useExisting: forwardRef(() => RegexpInputComponent),
    multi: true,
};


@Component({
    selector: "ig-regexp-input",
    templateUrl: "regexp-input.component.html",
    providers: [REGEXP_EDITOR_ACCESSOR, REGEXP_INPUT_VALIDATOR],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./regexp-input.component.less"]
})
export class RegexpInputComponent implements ControlValueAccessor, OnInit, Validator {
    @Input() showSamples: boolean = true;
    @Input() showValueValidator: boolean = true;

    @Input() disabled: boolean = false;
    @Input() required: boolean = false;

    value = "";

    hasError: boolean = false;
    validationMessage: string = "";

    expressionTestString = "";
    menuFocus: boolean = false;

    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };
    onValidationChange: Function = () => { };

    constructor(public ref: ChangeDetectorRef,
        public el: ElementRef) { }

    ngOnInit() {
    }

    examples = [
        { title: "Email", value: "^.+@.+..+$" },
        { title: "IP Address", value: "^(?:\\d{1,3}.){3}\\d{1,3}$" },
        { title: "North American Phone Number", value: "^\\(?([0-9]{3})\\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$" },
        { title: "URL", value: "^(https?:(//))?(\\S+.\\w{2,}\\b)((/)(\\S*))?$" },
        { title: "US Zip Code", value: "^[0-9]{5}(?:-[0-9]{4})?$" }
    ]

    setExample(title: string) {
        const example = this.examples.find((ex) => ex.title === title);
        this.writeValue(example?.value);
    }

    writeValue(obj: any): void {
        this.value = obj;
        this.onModelChange(this.value);
        this.onValidationChange();
        this.ref.markForCheck();
    }

    setValue(obj: any): void {
        this.writeValue(obj);
        this.onModelTouched();
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    registerOnValidatorChange(fn: () => void): void {
        this.onValidationChange = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    validate(control: AbstractControl): ValidationErrors {
        const error = this.getValidationError(control.value);
        this.hasError = (error != null);
        this.validationMessage = Object.values(error ?? {})[0] ?? '';
        return error;
    }

    getValidationError(value: string) {
        if (value == null || value.trim() === '') {
            if (this.required !== false) {
                return {
                    required: $localize`Value required`
                };
            }

            return null;
        }

        if (!this.isValidRegex(value)) {
            return {
                regexp: $localize`You have provided an invalid regular expression`
            };
        }

        if (!value.startsWith("^") || !value.endsWith("$")) {
            return {
                regexp: $localize`Regular expression should start with ^ anchor and end with $ anchor`
            };
        }

        return null;
    }

    isValidRegex(value: string) {
        let isValid = true;
        try {
            new RegExp(value);
        } catch (e) {
            isValid = false;
        }

        return isValid;
    }
}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        PopupMenuModule,
        TooltipModule,
        DirectivesModule
    ],
    declarations: [
        RegexpInputComponent,
        RegexpTesterComponent
    ],
    exports: [RegexpInputComponent]
})

export class RegexpInputModule { }
