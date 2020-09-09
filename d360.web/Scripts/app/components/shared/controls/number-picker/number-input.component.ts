import { NgModule,  ElementRef, ChangeDetectorRef, forwardRef, Component, ViewEncapsulation, Input, ViewChild, OnInit, EventEmitter, Output, OnChanges, SimpleChanges} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, NG_VALIDATORS, ValidationErrors, AbstractControl } from '@angular/forms';


export const NUMBER_INPUT_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => IgNumberFieldcomponent),
    multi: true
};

@Component({
    selector: 'ig-number-input',
    templateUrl: 'number-input.component.html',
    providers: [NUMBER_INPUT_ACCESSOR],
    styleUrls: ['number-picker.component.less'],
    encapsulation: ViewEncapsulation.None,
})
export class IgNumberFieldcomponent implements ControlValueAccessor, OnInit{

    @Input() placeholder: string;
    @Input() step: number;
    @Input() max: number;
    @Input() min: number;
    @Input() disabled: boolean = false;
    @Input() required: boolean = false;
    @Input() name: string;
    @Input() tabindex: number = 0;
    @Input() styleClass: string = '';


    protected value: number;
    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };
    @ViewChild('iginput', { static: false }) el: ElementRef;

    constructor(private ref: ChangeDetectorRef) { }
   

    writeValue(obj: any): void {
        this.value = obj;
        this.onModelChange(this.value);
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

    increment() {
        this.el.nativeElement.stepUp();
        this.writeValue(this.el.nativeElement.value);
    }
    decrement() {
        this.el.nativeElement.stepDown();
        this.writeValue(this.el.nativeElement.value);
    }

    getStyleClass(): string {
        return 'ig-number-field';
    }

    ngOnDestroy() {
        this.removeElementChildren(this.el);
    }

    removeElementChildren(elem: any) {
        while (elem.hasChildNodes()) {
            elem.removeChild(this.el.nativeElement.lastChild);
        }
    }
    onInputKeyDown(event) {
        switch (event.which) {
            case 13:
                this.onModelTouched();
            break;
        }
    }
}

@NgModule({
    imports: [CommonModule],
    exports: [IgNumberFieldcomponent],
    declarations: [IgNumberFieldcomponent]
})
export class IgNumberFieldModule { }