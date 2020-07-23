import { Component, Input, Output, EventEmitter, NgModule, forwardRef, } from '@angular/core';
import { BaseComponent } from '../../base.component';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconService } from '../../../../services/icon.service';
import { DropdownModule } from 'primeng/dropdown';

import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';


export const ICON_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => IconPickerComponent),
    multi: true
};



@Component({
    selector: 'd3s-icon-picker',
    templateUrl: 'icon-picker.component.html',
    styleUrls: ['icon-picker.component.css']
})

export class IconPickerComponent extends BaseComponent implements ControlValueAccessor {
    @Input() ngModel: string;
    @Output() ngModelChange = new EventEmitter();

    private categories: any = [];

    constructor(private iconService: IconService) {
        super();
        this.isLoading = true;
    }

    ngOnInit() {
        this.iconService.getIconProperties().subscribe(result => {
            result.forEach(i => {
                let index = this.categories.findIndex(x => x.label == i.categories[0]);

                if (index == -1) {
                    this.categories.push({
                        label: i.categories[0],
                        value: i.categories[0],
                        items: [{ label: i.name, value: 'fa-' + i.id }]
                    });
                } else {
                    this.categories[index].items.push({ label: i.name, value: 'fa-' + i.id });
                }
            });

            this.categories.forEach(c => c.items.sort((a, b) => this.sortByName(a, b)));
            this.categories.sort((a, b) => this.sortByName(a, b));

            this.isLoading = false;
        });
    }

    private sortByName(a, b) {
        return (a.label < b.label) ? -1 : (a.label > b.label) ? 1 : 0;
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
        IconPickerComponent
    ],
    exports: [
        IconPickerComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,
        DropdownModule,
    ],
    providers: [IconService]
})
export class IconPickerModule { }