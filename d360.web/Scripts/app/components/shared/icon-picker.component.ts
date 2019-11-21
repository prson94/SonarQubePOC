import { Component, Input, Output, EventEmitter, NgModule, } from '@angular/core';
import { BaseComponent } from './base.component';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconService } from '../../services/icon.service';

@Component({
    selector: 'd3s-icon-picker',
    template: `
<select name="icon" [ngModel]="ngModel" (ngModelChange)="ngModel=$event;ngModelChange.emit(ngModel);" style="width:100%">
    <option value=""></option>
    <optgroup *ngFor="let category of categories" [label]="category.name">
        <option *ngFor="let icon of category.icons" [value]="'fa-' + icon.id">{{icon.name}}</option>
    </optgroup>
</select>             
    `
})

export class IconPickerComponent extends BaseComponent {
    @Input() ngModel: string;
    @Output() ngModelChange = new EventEmitter();

    private categories: any = [];

    constructor(private iconService: IconService) {
        super();

        iconService.getIconProperties().subscribe(result => {
            result.forEach(i => {
                let index = this.categories.findIndex(x => x.name == i.categories[0]);

                if (index == -1) {
                    this.categories.push({
                        name: i.categories[0],
                        icons: [i]
                    });
                } else {
                    this.categories[index].icons.push(i);
                }
            });

            this.categories.forEach(c => c.icons.sort((a, b) => this.sortByName(a, b)));
            this.categories.sort((a, b) => this.sortByName(a, b)); 
        });
    }

    ngOnInit() {

    }

    private sortByName(a, b) {
        return (a.name < b.name) ? -1 : (a.name > b.name) ? 1 : 0;
    }
}

@NgModule({
    declarations: [
        IconPickerComponent
    ],
    exports: [
        IconPickerComponent
    ]
    , imports: [
        CommonModule,
        FormsModule
    ],
    providers: [IconService]
})
export class IconPickerModule { }