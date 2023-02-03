import {
    AfterViewInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    EventEmitter,
    forwardRef,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges,
    ViewChild,
    ViewEncapsulation
} from '@angular/core';
import { SelectItem } from 'primeng/api';
import { ControlValueAccessor, FormControl, NG_VALUE_ACCESSOR } from '@angular/forms';
import { Dropdown } from 'primeng/dropdown';

/*global $localize*/

export const COLORPICKER_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ColorPickerComponent),
    multi: true
};

@Component({
    selector: 'ig-color-picker',
    templateUrl: 'color-picker.component.html',
    providers: [COLORPICKER_VALUE_ACCESSOR],
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        "(click)": "focus($event)",
		'(blur)': 'focus($event)',
		'(focus)': 'focus($event)'
    }
})

export class ColorPickerComponent implements ControlValueAccessor, AfterViewInit, OnChanges, OnInit {

    @Input() colors: SelectItem[] = [];
    @Input() placeholder: string = $localize`Optional`;
    @Input() filterplaceholder: string = $localize`Search colors`;
    @Input() selectedColor: string = '';
    @Input() invalidOptions: string[] = [];
    @Input() disabled: boolean = false;
    @Input() styleClass: string = '';
    @Input() style: any;
    @Input() tabindex: number = 0;
	@Input() required;
	@Input() igSize: string = "medium";
	@Input() formControl: FormControl;

    @Output() selectedColorChange = new EventEmitter();

    onModelChange: Function = () => { };

    onModelTouched: Function = () => { };
	protected value: string;

	isRequired = false;

	labelRequired = $localize`Required`;
	labelOptional = $localize`Optional`;

    @ViewChild("dd", { static: false }) dropdown: Dropdown;

    constructor(private ref: ChangeDetectorRef) {
    }
    ngOnChanges(changes: SimpleChanges): void {
		if (changes["colors"]) {
            this.colors.forEach((x) => {
                if (this.invalidOptions.indexOf(x.value) !== -1) {
                    x.disabled = true;
                } else {
                    x.disabled = false;
                }
            });
        }
	}

	ngOnInit() {
		this.isRequired = typeof this.required !== "undefined";
	}

    ngAfterViewInit(): void {
        if (this.invalidOptions.indexOf(this.selectedColor) !== -1) {
			this.writeValue(null);
        }

        this.ref.markForCheck();
    }

    writeValue(obj: string): void {
		this.selectedColor = obj;
        this.value = obj;
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
        this.disabled = isDisabled;
    }

    itemChanged(item: any) {
		this.writeValue(item.value);
		if (this.formControl) {
			this.formControl.setValue(item.value, { emitEvent: true });
		}
    }

	public focus(evt) {
        this.dropdown.focus();
    }
}
