import { Input, Component, Output, SimpleChange, EventEmitter, OnInit, NgModule, ViewChild, ElementRef, forwardRef } from '@angular/core';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';


export const BOOLEAN_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => Boolean),
    multi: true
};


@Component({
    selector: 'igx-boolean',
    templateUrl: 'igx-boolean.html',
    providers: [BOOLEAN_VALUE_ACCESSOR],
})
export class Boolean implements ControlValueAccessor  {
    

    @Input() label: string;
    @Input() value: boolean = false;
    
    @Output() onchange: EventEmitter<any> = new EventEmitter();

    @Input() trueTitle: string;
    @Input() trueButtonText: string;
    @Input() trueText: string;
    @Input() falseTitle: string;
    @Input() falseButtonText: string;
    @Input() falseText: string;

    @ViewChild("switch", {static:false}) _el: ElementRef;

    tryChangeValue(val: boolean) {
        
        this._el.nativeElement.focus();

        if (val === this.value) return;       

    }


    writeValue(obj: any): void {
        throw new Error("Method not implemented.");
    }
    registerOnChange(fn: any): void {
        throw new Error("Method not implemented.");
    }
    registerOnTouched(fn: any): void {
        throw new Error("Method not implemented.");
    }
    setDisabledState?(isDisabled: boolean): void {
        throw new Error("Method not implemented.");
    }

}


@NgModule({
    declarations: [
        Boolean        
    ],
    exports: [
        Boolean
    ]

})

export class BooleanModule { }

