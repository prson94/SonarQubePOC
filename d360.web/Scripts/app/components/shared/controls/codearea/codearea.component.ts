import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, EventEmitter, Output, ViewChild, OnInit } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { EditorModule } from 'primeng/editor';
import { CodemirrorModule } from '@ctrl/ngx-codemirror';
import 'codemirror/mode/javascript/javascript';
import 'codemirror/mode/css/css';
import 'codemirror/addon/display/placeholder';

export const CODE_EDITOR_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => CodeArea),
    multi: true    
};

@Component({
    selector: "codearea",
    templateUrl: "codearea.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [CODE_EDITOR_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./codearea.component.less"]
})
export class CodeArea implements ControlValueAccessor, OnInit {
    @Input() placeholder: string;
    @Input() codeType: string = "json";
    @Input() disabled: boolean = false;
    @Input() readonly: boolean = false;
    @Input() required: boolean = false;
    @Input() igSize: string = "";    
    @Output() isValid = new EventEmitter();

    value = "";
    sizeClass: string = "ig-field-width-full";
    validationMessage: string = "JSON is not well formed.Please review and update.";

    editorConfig = {
        lineNumbers: true,
        theme: 'default',
        mode: { name: "javascript", json: true },
        placeholder: "",
        readOnly: false
    };

    codeTypeList = ["json", "css"];

    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    @ViewChild('codeComponent', { static: false }) codeComponent: ElementRef;

    constructor(public ref: ChangeDetectorRef,
        public el: ElementRef) { }

    ngOnInit() {
        this.placeholder = this.placeholder == null ? (this.required ? 'Value required' : 'Optional') : this.placeholder;
        this.editorConfig.placeholder = this.placeholder;

        if (this.igSize && this.igSize === "large") {
            this.sizeClass = "ig-field-width-large";
        }

        if (this.codeType !== 'json' && this.codeTypeList.indexOf(this.codeType.toLocaleLowerCase()) >= 0) {
            switch (this.codeType.toLocaleLowerCase()) {
                case "css":
                    this.editorConfig.mode = { name: "css", json: false };
                    this.validationMessage = "CSS is not well formed. Please review and update.";
                    break;
                default:
                    this.editorConfig.mode = { name: "javascript", json: true };
                    break;
            }
        }

        if (this.disabled || this.readonly) {
            this.editorConfig.readOnly = true;
        }
    }

    ngAfterViewInit() {
        if (this.required) {
            this.codeComponent.nativeElement.setAttribute("required");            
        }                
    }

    writeValue(obj: any): void {
        this.value = obj;
        this.ref.markForCheck();
        this.onModelChange(this.value);
    }

    registerOnChange(fn: any): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onModelTouched = fn;
    }

    get isCodeValid() {
        let valid = true;
        if (this.codeType.toLocaleLowerCase() === "json") {
            let json = this.value;            
            try {
                if (json && json.trim() !== "") {
                    let j = JSON.parse(json);
                } else if (this.required) {
                    valid = false;
                }                
            } catch (e) {
                valid = false;
            }               
        }
        this.isValid.emit({ isvalid: valid });
        return valid;
    }
}

@NgModule({
    imports: [
        CommonModule,
        EditorModule,
        FormsModule,
        ReactiveFormsModule,
        CodemirrorModule
    ],
    declarations: [CodeArea],
    exports: [CodeArea]
})

export class CodeAreaModule { }