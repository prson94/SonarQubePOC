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

    @Output() ngModelChange = new EventEmitter<Date>()

    constructor(
        protected ref: ChangeDetectorRef
    )
    {
    }
    
    ngOnInit(): void {        

    }

    get getStyleClass(): string {
        return this.styleClass == null ? 'ig-date' : this.styleClass + ' ig-date';
    }

}

@NgModule({
    imports: [CommonModule],
    declarations: [IgDate],
    exports: [IgDate]
})

export class IgDateModule { }
