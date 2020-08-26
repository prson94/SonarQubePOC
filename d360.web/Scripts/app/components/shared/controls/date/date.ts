import {
    Input,
    Component,
    Output,
    EventEmitter,
    OnInit,
    NgModule,
    ChangeDetectorRef,
    ViewEncapsulation,
    ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { CalendarModule } from 'primeng/calendar';
import { FormsModule, ReactiveFormsModule, FormGroup, AbstractControl } from '@angular/forms';


@Component({
    selector: 'ig-date',
    templateUrl: 'date.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./date.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class IgDate implements OnInit  {        
    @Input() ngModel: Date;
    @Input() style: string;
    @Input() errorLabel: string;
    @Input() styleClass: string;
    @Input() inputStyle: string;
    @Input() inputStyleClass: string;
    @Input() placeholder: string;
    @Input() disabled: boolean = false;
    @Input() required: boolean = false;
    @Input() appendTo: string;
    @Input() minDate: Date;
    @Input() maxDate: Date;
    @Input() dateFormat: string = "mm/dd/yy";
    @Input() name: string;
    @Input() label: string;
    @Input() form: FormGroup;

    @Output() ngModelChange = new EventEmitter<Date>()
    protected formControl: AbstractControl;

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

    get formControlError(): boolean {
        if (this.form != null && this.form.contains(this.name)) {
            let control = this.form.get(this.name);
            return (!control.valid && control.dirty && control.touched);
        }

        return false;

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
