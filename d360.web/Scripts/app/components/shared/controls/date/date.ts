import {
    Input,
    Component,
    Output,
    EventEmitter,
    OnInit,
    NgModule,
    ChangeDetectorRef,
    ViewEncapsulation,
    ChangeDetectionStrategy,
    forwardRef,
    ViewChild
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarModule, Calendar } from 'primeng/calendar';
import {
    FormsModule,
    ReactiveFormsModule,
    NG_VALUE_ACCESSOR,
    ControlValueAccessor
} from '@angular/forms';

export const IG_DATE_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => IgDate),
    multi: true
};


@Component({
    selector: 'ig-date',
    templateUrl: 'date.html',
    encapsulation: ViewEncapsulation.None,
    providers: [IG_DATE_VALUE_ACCESSOR],
    styleUrls: ['./date.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "(click)": "focus($event)",
        '(focus)': 'focus($event)',
    }
})
export class IgDate implements ControlValueAccessor, OnInit  {        
    @Input() style: string;
    @Input() styleClass: string;
    @Input() inputStyle: string;
    @Input() inputStyleClass: string;
    @Input() placeholder: string;
    @Input() disabled: boolean = false;
    @Input() required: boolean = false;
    @Input() appendTo: string;
    @Input() tabindex: number = 0;
    @Input() minDate: Date;
    @Input() maxDate: Date;
    @Input() dateFormat: string = "mm/dd/yy";
    @Input() name: string;
    @Input() label: string;


    @ViewChild("cal", { static: false }) calendar: Calendar;

    protected value = null;

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };

    constructor(
        protected ref: ChangeDetectorRef
    )
    {
    }
    
    ngOnInit(): void {        
        this.placeholder = this.placeholder == null ? (this.required ? 'Value required' : 'Optional') : this.placeholder;
    }

    get getStyleClass(): string {
        return this.styleClass == null ? 'ig-date' : this.styleClass + ' ig-date';
    }

    get getInputStyleClass(): string {
        return this.inputStyleClass == null ? 'ig-date ig-input' : this.inputStyleClass + ' ig-date ig-input';

    }

    tryChangeValue(val: boolean) {
        if (!this.disabled) {
            this.writeValue(val);
        }
    }

    writeValue(obj: any): void {
        this.value = obj;
        this.onModelChange(this.value);
        this.ref.markForCheck();
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
        this.calendar.inputfieldViewChild.nativeElement.focus();
    }
}

@NgModule({
    imports: [
        CommonModule,
        CalendarModule,
        FormsModule,
        ReactiveFormsModule,
    ],
    declarations: [
        IgDate
    ],
    exports: [
        IgDate
    ],
})

export class IgDateModule { }
