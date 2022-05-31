import { Input, Component, Output, EventEmitter, OnInit, NgModule, ViewChild, ElementRef, forwardRef, ChangeDetectorRef, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';


export const SWITCH_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => Switch),
    multi: true
};


@Component({
    selector: 'ig-switch',
    templateUrl: 'switch.html',
    providers: [SWITCH_VALUE_ACCESSOR],
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./switch.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "(click)": "focus($event)",
        '(focus)': 'focus($event)',
    }
})
export class Switch implements ControlValueAccessor, OnInit {
    @Input() trueLabel = $localize`Yes`;

    @Input() falseLabel = $localize`No`;

    @Input() optional: boolean = false;

    @Input() disabled = false;

    @Input() styleClass: string;

    @Input() style: any;

    @Input() tabindex: number = 0;

    @Input() inputId: string;

    @Input() ariaLabelledBy: string;

    @Output() onChange: EventEmitter<any> = new EventEmitter();

    value = false;  // this is intentionally NOT an input you should be using ngModel..

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };

    private isInitialValueSet: boolean = false;

    constructor(
        protected changeDetectorRef: ChangeDetectorRef
    ) {
    }

    ngOnInit(): void {
        if (!this.trueLabel || this.trueLabel.length > 5) {
            console.error("Invalid use of switch component true label should be 5 or less characters and not null")
        }
        if (!this.falseLabel || this.falseLabel.length > 5) {
            console.error("Invalid use of switch component true label should be 5 or less characters and not null")
        }
    }

    @ViewChild("switch", { static: false }) _el: ElementRef;

    toggle(e: Event) {
        this.tryChangeValue(this.value === undefined ? true : !this.value);
        e.preventDefault();
    }

    tryChangeValue(val: boolean) {
        if (!this.disabled) {
            this.writeValue(val);
        }
    }

    writeValue(obj: boolean): void {
        if (this._el) this._el.nativeElement.focus();

        if (!this.optional && (obj === this.value)) {     // not optional and current value = previous   
            return;
        }
        else if (this.optional && (obj === this.value) && this.isInitialValueSet) {      // optional and current value = previous  
            this.value = undefined;
        }
        else {
            this.value = obj;
        }

        this.onModelChange(this.value);
        this.onChange.emit(this.value);
        this.isInitialValueSet = true;
        this.changeDetectorRef.markForCheck();
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled
    }
    public focus(evt) {
        if (this._el) this._el.nativeElement.focus();
    }
}

@NgModule({
    imports: [CommonModule],
    declarations: [Switch],
    exports: [Switch]
})

export class SwitchModule { }
