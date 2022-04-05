import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, OnInit, OnChanges, SimpleChanges } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR, Validator, AbstractControl, ValidationErrors } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { DirectivesModule } from "../../../../directives/directives.module";
import { ButtonModule } from "../../../../directives/ig-button-directive";
import { TooltipModule } from "primeng/tooltip";
import { DomSanitizer } from '@angular/platform-browser';
import { ColorPickerModule } from "primeng/colorpicker";

export const COLOR_SELECTOR_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ColorSelector),
    multi: true
};

@Component({
    selector: "color-selector",
    templateUrl: "color-selector.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [COLOR_SELECTOR_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./color-selector.component.less"]
})
export class ColorSelector implements ControlValueAccessor {
    @Input() appendTo: string = '';
    value = "";
    textBoxValue = "";

    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    constructor(public ref: ChangeDetectorRef,
        public domSanitizer: DomSanitizer,
        public el: ElementRef) {
    }

    writeValue(obj: any): void {
        this.value = obj;
        this.textBoxValue = obj;
        this.ref.markForCheck();
        this.onModelChange(this.value);
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    onChange($event) {
        this.writeValue($event.value);
    }

    onEnter($event) {
        var hexCode = "";
        if (!this.textBoxValue.startsWith("#")) {
            this.textBoxValue = this.value;
            return;
        }
        hexCode = this.textBoxValue.split("#")[1];

        if (!this.allEqual(hexCode) && hexCode.length === 5) {
            hexCode = hexCode + "0";
        }

        if (this.allEqual(hexCode) && hexCode.length === 5) {
            hexCode = hexCode.slice(0, 2).repeat(3);
        }

        if (!this.allEqual(hexCode) && this.hasPairs(hexCode)) {
            hexCode = hexCode.repeat(2).slice(0, 6);
        }

        if (hexCode.length === 4 && !this.allEqual(hexCode) && !this.hasPairs(hexCode)) {
            hexCode = hexCode + "00";
        }

        if (this.allEqual(hexCode) && hexCode.length === 4) {
            hexCode = hexCode.slice(0, 2).repeat(3);
        }

        if (hexCode.length === 3 && this.allEqual(hexCode)) {
            hexCode = hexCode.repeat(2);
        }

        if (hexCode.length === 3 && !this.allEqual(hexCode)) {
            hexCode = hexCode[0].repeat(2) + hexCode[1].repeat(2) + hexCode[2].repeat(2);
        }

        if (hexCode.length === 2) {
            hexCode = hexCode.repeat(3);
        }

        if (hexCode.length === 1) {
            hexCode = hexCode.repeat(6);
        }

        if (this.isHexColor(hexCode)) {
            this.writeValue("#" + hexCode);
        }
        else {
            this.textBoxValue = this.value;
        }
    }

    private isHexColor(hex) {
        return typeof hex === 'string'
            && hex.length === 6
            && !isNaN(Number('0x' + hex));
    }

    private allEqual(input) {
        return input.split('').every((char) => char === input[0]);
    }

    private hasPairs(input: string) {
        return input.length === 4 && input[0] === input[2] && input[1] && input[3];
    }
}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DirectivesModule,
        ButtonModule,
        TooltipModule,
        ColorPickerModule
    ],
    declarations: [ColorSelector],
    exports: [ColorSelector]
})

export class ColorSelectorModule { }