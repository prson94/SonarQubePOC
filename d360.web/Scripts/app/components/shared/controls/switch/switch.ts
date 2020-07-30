import { Input, Component, Output, EventEmitter, OnInit, NgModule, ViewChild, ElementRef, forwardRef, ChangeDetectorRef, HostBinding, ViewEncapsulation, ChangeDetectionStrategy } from '@angular/core';
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
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class Switch implements ControlValueAccessor, OnInit  {        
    @Input() trueLabel = "Yes";

    @Input() falseLabel = "No";

    @Input() alwaysSet: boolean = true;

    @Input() disabled = false;

    @Input() styleClass: string;

    @Input() style: any;

    @Input() tabindex: number = 0;

    @Input() inputId: string;

    @Input() ariaLabelledBy: string;

    @Output() onChange: EventEmitter<any> = new EventEmitter();

    protected value = false;  // this is intentionally NOT public or an input you should be using ngModel..

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };
    

    constructor(protected changeDetectorRef: ChangeDetectorRef) { }


    ngOnInit(): void {        
        if (!this.trueLabel || this.trueLabel.length > 5) {
            console.error("Invalid use of switch component true label should be 5 or less characters and not null")
        }    
        if (!this.falseLabel || this.falseLabel.length > 5) {
            console.error("Invalid use of switch component true label should be 5 or less characters and not null")
        }    
    }

    @HostBinding('style.opacity')
    get opacity() {
        return this.disabled ? 0.33 : 1;
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
        
        if (this.alwaysSet && (obj === this.value)) {        
            return;
        }
        else if (!this.alwaysSet && (obj === this.value)) {            
            this.value = undefined;
        }
        else {
            this.value = obj;
        }

        this.onModelChange(this.value);
        this.onChange.emit(this.value);
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
}

@NgModule({
    imports: [CommonModule],
    declarations: [Switch],
    exports: [Switch]
})

export class SwitchModule { }
