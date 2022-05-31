
import { Component, OnInit, EventEmitter, Output, Input, forwardRef, ChangeDetectorRef, ChangeDetectionStrategy, ViewEncapsulation, AfterViewInit, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import * as _ from 'lodash';
import { SelectItem } from 'primeng/api';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';
import { Dropdown } from 'primeng/dropdown';

export const COLORPICKER_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ColorPickerComponent),
    multi: true
};

@Component({
    selector: 'ig-color-picker',
    template: `
                <div [ngStyle]="style" [class]="'d3s-color-picker ' + styleClass" tabindex="-1">
                    <p-dropdown [class]="'p-dropdown-wrapper'" #dd [tabIndex]="tabindex" [appendTo]="'body'" [options]="colors" [panelStyleClass]="'igx-blue'" [ngModel]="selectedColor" (onChange)="itemChanged($event)" placeholder="{{placeholder}}" scrollHeight="320px" showClear="true" filter="true" filterPlaceholder="{{filterplaceholder}}" [disabled]="disabled" (focus)="focus($event)">
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
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "(click)": "focus($event)",
        '(blur)': 'focus($event)',
    }
})

export class ColorPickerComponent implements ControlValueAccessor, AfterViewInit, OnChanges {

    @Input() colors: SelectItem[] = [];
    @Input() placeholder: string = $localize`Optional`;
    @Input() filterplaceholder: string = $localize`Search colors`;
    @Input() selectedColor: string;
    @Input() invalidOptions: string[] = [];
    @Input() disabled: boolean = false;
    @Input() styleClass: string = '';
    @Input() style: any;
    @Input() tabindex: number = 0;

    @Input() igSize: string = '';

    @Output() selectedColorChange = new EventEmitter();

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };
    protected value: string;

    @ViewChild("dd", { static: false }) dropdown: Dropdown;

    constructor(private ref: ChangeDetectorRef) {
    }
    ngOnChanges(changes: SimpleChanges): void {
        if (changes["colors"]) {
            this.colors.forEach(x => {
                if (this.invalidOptions.indexOf(x.value) != -1) {
                    x.disabled = true;
                } else {
                    x.disabled = false;
                }
            });
        }
    }

    ngAfterViewInit(): void {
        if (this.invalidOptions.indexOf(this.selectedColor) != -1) {
            this.writeValue(null);
        }

        //set igSize
        if (this.igSize && this.igSize === "small") {
            this.styleClass += "ig-input-small";
        } else if (this.igSize && this.igSize === "medium") {
            this.styleClass += "ig-input-medium";
        } else if (this.igSize && this.igSize === "large") {
            this.styleClass += "ig-input-large";
        } else if (this.igSize && this.igSize === "full") {
            this.styleClass += "ig-input-full";
        }

        this.ref.markForCheck();
    }

    writeValue(obj: string): void {
        this.selectedColor = obj;
        this.value = obj
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

    itemChanged(item: any) {
        this.writeValue(item.value)
    }

    public focus(evt) {
        this.dropdown.focus();
    }
}
