import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, EventEmitter, Output } from "@angular/core";
import { CommonModule } from "@angular/common";
import { TooltipModule } from "primeng/tooltip";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR } from "@angular/forms";
import { IgBadgeModule } from "../badge/badge.module";

export const IG_MULTIINPUTFIELD_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => MultiInputField),
    multi: true
};

@Component({
    selector: "ig-multi-input-field",
    templateUrl: "multi-input-field.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [IG_MULTIINPUTFIELD_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./multi-input-field.component.less"]
})
export class MultiInputField implements ControlValueAccessor {
    @Input() required: boolean = false;
    @Input() tabindex: number = 1;
    @Input() infoTooltip: string = "";
    @Output() changed = new EventEmitter();
    public _size: string;


    chips: string[] = [];

    public currentText: string = "";
    private disabled: boolean = false;
    private isInFocus: boolean = false;


    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    constructor(public ref: ChangeDetectorRef,
        public el: ElementRef) { }

    removeChip(item: string) {
        let idx: number = (this.value as string[]).indexOf(item);
        (this.value as string[]).splice(idx, 1);
        this.writeValue(this.value);
    }

    addChip(item: string) {
        var val = item.trim();
        if (val.length === 0) {
            return;
        }
        if (!this.value) {
            this.value = [];
        }

        (this.value as string[]).push(val);
        this.currentText = "";
        this.writeValue(this.value);

    }

    getInputBoxSize() {
        var size = this.currentText.length;
        if (size < 3) {
            size = 3;
        }
        return size - 1;
    }

    onKeyPress(event: KeyboardEvent) {
        if (event.keyCode === 13) {
            this.addChip(this.currentText);
        }
    }

    onControlClick($event: MouseEvent) {
        var target = $event.target as HTMLElement;
        if (target?.classList.contains("chips")) {
            target.getElementsByTagName("input")[0].focus();
        }
    }

    getElementClass() {
        let classes: string[] = ["ig-multi-input-field"];
        if (this.disabled) {
            classes.push("disabled");
        }
        if ((!this.chips || this.chips.length === 0) && this.currentText.length === 0) {
            classes.push("no-value");
        }
        if (this.required) {
            classes.push("required");
        }
        if (this.isInFocus) {
            classes.push("in-focus");
        }
        if (this.infoTooltip) {
            classes.push("has-tooltip");
        }

        if (this._size && this._size === "small") {
            classes.push("ig-input-small");
        } else if (this._size && this._size === "medium") {
            classes.push("ig-input-medium");
        } else if (this._size && this._size === "large") {
            classes.push("ig-input-large");
        } else if (this._size && this._size === "full") {
            classes.push("ig-input-full");
        }

        if (this.required) {
            this.el.nativeElement.setAttribute("aria-required", true);
        }

        return classes.join(" ");
    }

    get value(): string[] {
        return this.chips;
    }

    set value(param) {
        this.chips = param;
    }

    hasFocus(value: boolean) {
        this.isInFocus = value;
    }

    @Input() get igSize(): string {
        return this._size;
    }
    set igSize(val: string) {
        this._size = val;
    }

    //ControlAccessor Implementation
    writeValue(obj: any): void {
        this.value = obj;
        this.ref.markForCheck();
        this.onModelChange(this.value);
        this.changed.emit(this.value);
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

    public clearTextValue() {
        this.currentText = "";
        this.ref.markForCheck();
    }

    get placeholderText(): string {
        return this.required ? $localize`Value required` : `Optional`;
    }
}

@NgModule({
    imports: [
        CommonModule,
        TooltipModule,
        FormsModule,
        IgBadgeModule,
        ReactiveFormsModule,
    ],
    declarations: [MultiInputField],
    exports: [MultiInputField]
})

export class MultiInputFieldModule { }