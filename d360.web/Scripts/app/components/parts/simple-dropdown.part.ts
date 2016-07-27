///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, OnInit, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'simple-dropdown',
    template: `
        <select [(ngModel)]="item" (change)="select()">
            <option *ngFor="let i of items" [value]="i[valueProperty]">{{i[labelProperty]}}</option>
        </select>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SimpleDropdown implements OnInit {
    @Input() items: any[];
    @Input() labelProperty: string = 'label';
    @Input() valueProperty: string = 'value';
    @Input() item: any;
    @Output() itemChange = new EventEmitter();


    constructor() {
    }

    ngOnInit() {

    }

    select() {
        this.itemChange.emit(this.item);
    }


}

