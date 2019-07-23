import {Input, Component,Output, SimpleChange, EventEmitter} from '@angular/core';


@Component({
    selector: 'd3s-checkbox',
    templateUrl: 'gov-checkbox.html'
})

export class D3SCheckbox  {
    @Input() label: string;
    @Input() value: boolean = false;

    @Output() onchange: EventEmitter<any> = new EventEmitter();;


    changeValue(val: boolean) {
        this.value = val;
        this.onchange.emit(this.value);
    }
}

