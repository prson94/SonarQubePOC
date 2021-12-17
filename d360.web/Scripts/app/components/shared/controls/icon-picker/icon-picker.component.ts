import { ChangeDetectorRef, Component, Input, Output, EventEmitter, NgModule, forwardRef, ViewEncapsulation, } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { IconService } from '../../../../services/icon.service';
import { DropdownModule } from 'primeng/dropdown';
import { NG_VALUE_ACCESSOR, ControlValueAccessor } from '@angular/forms';

export const ICON_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => IconPickerComponent),
    multi: true
};

@Component({
    selector: 'ig-icon-picker',
    templateUrl: 'icon-picker.component.html',
    providers: [ICON_VALUE_ACCESSOR],
    styleUrls: ['icon-picker.component.less'],
    encapsulation: ViewEncapsulation.None,
})

export class IconPickerComponent implements ControlValueAccessor {
    @Input() ngModel: string;
    @Input() tabindex: number = 0;
    @Input() disabled: boolean = false;
    @Input() showGovIcons: boolean = false;
    @Input() required;
    @Input() style: any;

    @Output() ngModelChange = new EventEmitter();

    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    protected isRequired = false;
    protected categories: any = [];
    public isLoading: boolean = true;

    constructor(private iconService: IconService,
                private cdRef: ChangeDetectorRef) {
    }

    ngOnInit() {
        this.isRequired = this.required !== undefined;
        this.iconService.getIconProperties().subscribe(result => {
            this.iconService.getIconImages().subscribe(images => {
                if (this.showGovIcons)
                    result = [...result, ...images];

                result.forEach(i => {
                    let index = this.categories.findIndex(x => x.label == i.categories[0]);
                    if (!i.img) {
                        if (index == -1) {
                            this.categories.push({
                                label: i.categories[0],
                                value: i.categories[0],
                                items: [{ label: i.name, value: "fa-" + i.id }]
                            });
                        } else {
                            this.categories[parseInt(index)].items.push({ label: i.name, value: "fa-" + i.id });
                        }
                    }
                    else {
                        if (index == -1) {
                            this.categories.push({
                                label: i.categories[0],
                                value: i.categories[0],
                                items: [{ label: i.name, value: i.path, path: i.path, img: i.img }]
                            });
                        } else {
                            this.categories[parseInt(index)].items.push({ label: i.name, value: i.path, path: i.path, img: i.img });
                        }
                    }
                });

                this.categories.forEach(c => c.items.sort((a, b) => this.sortByName(a, b)));
                this.categories.sort((a, b) => this.sortByName(a, b));
                this.isLoading = false;
                this.cdRef.markForCheck();
            })
        });
    }

    private sortByName(a, b) {
        return (a.label < b.label) ? -1 : (a.label > b.label) ? 1 : 0;
    }

    writeValue(obj: string): void {
        this.onModelChange(obj);
    }
    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }
    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }
    setDisabledState?(isDisabled: boolean): void {
        this.disabled = isDisabled;
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
        ReactiveFormsModule,
        DropdownModule,
    ],
    providers: [IconService]
})
export class IconPickerModule { }