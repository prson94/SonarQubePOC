///<reference path="../es6-shim.d.ts"/>
import {Input, Component, EventEmitter, Output } from '@angular/core';

@Component({
    selector: 'multi-select',
    template: `
<div style="color:#777;">{{selectedItemsString}}</div>
<div style="height:150px;width:350px;overflow-y:scroll;">
    <div *ngFor="let item of data; let i=index;" (click)="selectItem(i)" [class]="'item ' + (data[i].Selected ? 'selected' : '')" style="cursor:pointer;">
        <input type="checkbox" [(ngModel)]="data[i].Selected" /> {{item.Text}}
    </div>
</div>
    `,
    styles: [
        `
.selected {
    background-color: #ddd;
}
.item:hover {
    background-color: #eee;
}
`
    ]
})

export class MultiSelect  {
    @Input() height: number = 150;
    @Input() width: number = 350;
    @Input() data: any[];
    @Output() onValueChanged = new EventEmitter();

    private selectItem(i) {
        this.data[i].Selected = !this.data[i].Selected;
        this.onValueChanged.emit(this.data.filter(d => d.Selected));
    }

    get selectedItems(): any[] {
        return this.data.filter(d => d.Selected);
    }

    get selectedItemsString(): string {
        return this.selectedItems.map(i => i.Text).join(', ');
    }
}