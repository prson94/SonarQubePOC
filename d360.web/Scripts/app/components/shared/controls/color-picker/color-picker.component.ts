
import { Component, OnInit, EventEmitter, Output, Input, forwardRef, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { ViewEncapsulation } from '@angular/compiler/src/compiler_facade_interface';

export const COLORPICKER_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ColorPickerComponent),
    multi: true
};

@Component({
    selector: 'ig-color-picker',
    template: `
                <div [ngStyle]="style" [class]="'d3s-color-picker ' + styleClass">
                    <p-dropdown [tabIndex]="tabindex" [appendTo]="'body'" [options]="colors" [panelStyleClass]="'igx-blue'" [ngModel]="selectedColor" (onChange)="itemChanged($event)" placeholder="{{placeholder}}" scrollHeight="320px" showClear="true" filter="true" filterPlaceholder="Search colors" [disabled]="disabled">
                        <ng-template let-item pTemplate="selectedItem">
                            <div class="ig-colorfield-item-selected">
                                <span class="ig-colorfield-swatch" [style.background-color]="item?.title"></span>
                                <span class="ig-colorfield-item-label">{{item?.label}}</span>
                            </div>
                        </ng-template>
                        <ng-template let-color pTemplate="item">
                            <div class="ig-colorfield-item">
                                <span class="ig-colorfield-swatch" [style.background-color]="color.title"></span>
                                <span class="ig-colorfield-item-label">{{color.label}}</span>
                            </div>
                        </ng-template>
                    </p-dropdown>
                </div>
			  `,
    providers: [COLORPICKER_VALUE_ACCESSOR],
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./color-picker.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class ColorPickerComponent implements ControlValueAccessor, OnInit {

    @Input() colors: SelectItem[] = [];
    @Input() placeholder: string = 'Optional';
    @Input() selectedColor: string;
    @Input() disabled: boolean = false;
    @Input() styleClass: string = '';
    @Input() style: any;
    @Input() tabindex: number = 0;

    @Output() selectedColorChange = new EventEmitter();

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };
    protected value: string;

    constructor(private ref: ChangeDetectorRef) {
    }

    writeValue(obj: string): void {

        this.selectedColor = obj;
        this.onModelChange(this.selectedColor);
        this.selectedColorChange.emit(this.selectedColor);
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


    ngOnInit() {

    }

    private itemChanged(item: any) {
        this.onModelChange(this.selectedColor);
        this.selectedColor = item.value;
        this.selectedColorChange.emit(item.value);
    }
};
