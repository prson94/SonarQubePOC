import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, OnInit, OnChanges, SimpleChanges } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR, Validator, AbstractControl, ValidationErrors } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { DirectivesModule } from "../../../../directives/directives.module";
import { ButtonModule } from "../../../../directives/ig-button-directive";
import { TooltipModule } from "primeng/tooltip";
import { DomSanitizer } from '@angular/platform-browser';
import { ColorPickerModule } from "primeng/colorpicker";
import { forEach } from "core-js/js/array";

export class ColorSelecterEvaluator {
    evaluator: Function;
    result: Function;
}

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

    valueEvaluators: ColorSelecterEvaluator[] = [];

    constructor(public ref: ChangeDetectorRef,
        public domSanitizer: DomSanitizer,
        public el: ElementRef) {
        this.populateEvaluators();
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

        this.valueEvaluators.forEach((e) => {
            if (e.evaluator(hexCode)) {
                hexCode = e.result(hexCode);
            }
        });

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

    private populateEvaluators() {
        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #12345
                return !this.allEqual(value) && value.length === 5;
            },
            result: (value: string) => {
                //output #123456
                return value + "0";
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #11111
                return this.allEqual(value) && value.length === 5;
            },
            result: (value: string) => {
                //output #111111
                return value.slice(0, 2).repeat(3);
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #1212
                return !this.allEqual(value) && this.hasPairs(value);
            },
            result: (value: string) => {
                //output #121212
                return value.repeat(2).slice(0, 6);
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #1234
                return value.length === 4 && !this.allEqual(value) && !this.hasPairs(value);
            },
            result: (value: string) => {
                //output #123400
                return value + "00";
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #1111
                return this.allEqual(value) && value.length === 4;
            },
            result: (value: string) => {
                //output #111111
                return value.slice(0, 2).repeat(3);
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #222
                return value.length === 3 && this.allEqual(value);
            },
            result: (value: string) => {
                //output #222222
                return value.repeat(2);
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #121
                return value.length === 3 && !this.allEqual(value);
            },
            result: (value: string) => {
                //output #112211
                return value[0].repeat(2) + value[1].repeat(2) + value[2].repeat(2);
            }
        });

        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #11 or #12
                return value.length === 2;
            },
            result: (value: string) => {
                //output #111111 or #121212
                return value.repeat(3);
            }
        });
        this.valueEvaluators.push({
            evaluator: (value: string) => {
                //input #1
                return value.length === 1;
            },
            result: (value: string) => {
                //output #111111
                return value.repeat(6);
            }
        });
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