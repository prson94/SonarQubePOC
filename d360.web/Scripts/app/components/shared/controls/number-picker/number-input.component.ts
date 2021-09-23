import { NgModule, ElementRef, ChangeDetectorRef, forwardRef, Component, ViewEncapsulation, Input, ViewChild, OnInit, EventEmitter, Output, OnChanges, SimpleChanges, HostListener, DoCheck } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, NG_VALIDATORS, Validator, ValidationErrors, AbstractControl, FormsModule } from '@angular/forms';


export const NUMBER_INPUT_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => IgNumberFieldcomponent),
    multi: true
};
export const NUMBER_INPUT_VALIDATOR: any = {
    provide: NG_VALIDATORS,
    useExisting: forwardRef(() => IgNumberFieldcomponent),
    multi: true,
};

@Component({
    selector: 'ig-number-input',
    templateUrl: 'number-input.component.html',
    providers: [NUMBER_INPUT_ACCESSOR, NUMBER_INPUT_VALIDATOR],
    styleUrls: ['number-picker.component.less'],
    encapsulation: ViewEncapsulation.None,
})
export class IgNumberFieldcomponent implements ControlValueAccessor, OnInit, Validator {

    @Input() placeholder: string;
    @Input() step: string = "any";
    @Input() max: number = 9223372036854775807;
    @Input() min: number;
    @Input() disabled: boolean = false;
    @Input() required: boolean = false;
    @Input() name: string;
    @Input() tabindex: number = 0;
    @Input() styleClass: string = '';
    @Input() ariaLabel: string;
    @Input() ariaRequired: boolean;
    @Input() ariaInvalid: boolean;

    @Input() enforceMaxMin: boolean = false;

    public _size: string = "small";
    @Input() get igSize(): string {
        return this._size;
    }
    set igSize(val: string) {
        this._size = val;
    }

    hasValue: boolean = false;

    value: number;
    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };
    onValidationChange: Function = () => { };
    @ViewChild('iginput', { static: false }) el: ElementRef;

    constructor(private ref: ChangeDetectorRef) { }

    writeValue(obj: any): void {
        if (obj != undefined && obj != null) {
            this.hasValue = true;
            if (this.enforceMaxMin) {
                var value = +this.el.nativeElement.value;
                if (this.max && value > this.max) {
                    value = this.max;
                    this.el.nativeElement.value = value;
                }
                if (this.min && value < this.min) {
                    value = this.min;
                    this.el.nativeElement.value = value;
                }
                obj = value;
            }
        }
        else {
            this.hasValue = false;
            this.value = null;
        }
        this.value = obj;
        this.onModelChange(this.value);
        this.onValidationChange();
        this.onModelTouched();
        this.ref.markForCheck();
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    ngOnInit(): void {
        this.placeholder = this.placeholder == null ? (this.required ? 'Value required' : 'Optional') : this.placeholder;
    }

    validate(control: AbstractControl): ValidationErrors {
        let result: any = null;
        if (isNaN(parseInt(control.value))) {
            result = null;
        } else if (this.isOverMax()) {
            result = {
                overMax: {
                    actual: +this.value,
                    max: +this.max
                }
            };
        } else if (this.isUnderMin()) {
            result = {
                underMin: {
                    actual: +this.value,
                    min: +this.min
                }
            };
        }
        return result;
    }

    private isOverMax(): boolean {
        return this.value !== null && typeof this.max !== "undefined" && this.value > +this.max;
    }

    private isUnderMin(): boolean {
        return this.value !== null && typeof this.min !== "undefined" && this.value < +this.min;
    }

    registerOnValidatorChange?(fn: () => void): void {
        this.onValidationChange = fn;
    }

    increment() {
        if (!this.disabled) {
            if (this.step === "any") {
                var currentValue = +this.el.nativeElement.value + 1;
                this.el.nativeElement.value = currentValue.toString();
                this.writeValue(this.el.nativeElement.value);
            }
            else {
                this.el.nativeElement.stepUp();
                this.writeValue(this.el.nativeElement.value);
            }
        }
    }
    decrement() {
        if (!this.disabled) {
            if (this.step === "any") {
                var currentValue = +this.el.nativeElement.value - 1;
                this.el.nativeElement.value = currentValue.toString();
                this.writeValue(this.el.nativeElement.value);
            }
            else {
                this.el.nativeElement.stepDown();
                this.writeValue(this.el.nativeElement.value);
            }
        }
    }

    getStyleClass(): string {
        return 'ig-number-field ' + this.styleClass;
    }
    getElementClass() {
        let classes: string[] = ["ig-number-input"];
        if (["small", "medium", "large", "full"].indexOf(this._size) !== -1) {
            classes.push("ig-input-" + this._size);
        }
        return classes.join(" ");
    }

    onInputKeyDown(event) {
        switch (event.which) {
            case 13:
                this.onModelTouched();
                break;
        }
    }
    @HostListener('click')
    clickInside($event) {
        this.el.nativeElement.focus();
    }

}

@NgModule({
    imports: [CommonModule, FormsModule],
    exports: [IgNumberFieldcomponent],
    declarations: [IgNumberFieldcomponent]
})
export class IgNumberFieldModule { }
