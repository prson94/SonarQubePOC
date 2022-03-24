import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, EventEmitter, Output, ViewChild, OnInit } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { DirectivesModule } from "../../../../directives/directives.module";
import { ButtonModule } from "../../../../directives/ig-button-directive";

export const IMAGE_PICKER_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ImagePicker),
    multi: true
};

@Component({
    selector: "image-picker",
    templateUrl: "image-picker.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [IMAGE_PICKER_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./image-picker.component.less"]
})
export class ImagePicker implements ControlValueAccessor, OnInit {
    @Input() message = 'Choose an ICO file to display in the browser tab/taskbar';
    @Input() imageType = '';
    @Input() allowedExtensions = '';
    @Input() maxHeight: number;
    @Input() maxWidth: number;
    @Input() maxSize: number;

    accept = "";
    value = "";
    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    previewHeight: number = 0;
    previewWidth: number = 0;

    isFormatValid: boolean = true;
    isDimensionValid: boolean = true;

    invalidFormatMessage: string = 'File type not supported. Please choose a PNG, JPG or GIF image.';
    invalidDimensionMessage: string = 'File exceeds height [or width] limit. Please upload a file that has max file height of [40px].';

    constructor(public ref: ChangeDetectorRef,
        public el: ElementRef) { }

    ngOnInit() {
        if (!this.allowedExtensions) {
            this.accept = "image/png,image/gif,image/jpg,image/jpeg";
        }

        if (this.imageType === 'ICO') {
            this.previewHeight = 40;
            this.previewWidth = 40;
            this.accept = "image/ico,image/x-icon";
            this.invalidFormatMessage = 'File type not supported. Please choose a ICO image.';
        }
        else if (this.imageType === 'LOGO') {
            this.previewHeight = 40;
            this.previewWidth = 168;
        }
        else {
            this.previewHeight = 96;
            this.previewWidth = 168;
        }

        if (this.maxHeight) {
            this.invalidDimensionMessage = `File exceeds height limit. Please upload a file that has max file height of [${this.maxHeight}px].`;
        }
        if (this.maxWidth) {
            this.invalidDimensionMessage = `File exceeds width limit. Please upload a file that has max file width of [${this.maxWidth}px].`;
        }
        if (this.maxHeight && this.maxWidth) {
            this.invalidDimensionMessage = `File exceeds height or width limit. Please upload a file that has max file dimensions of [${this.maxWidth}px X ${this.maxHeight}px].`;
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

    clearValue() {
        this.value = "";
        this.isFormatValid = true;
        this.isDimensionValid = true;
        this.onModelChange(this.value);
        this.ref.markForCheck();
    }

    handleInputChange(e) {
        this.value = "";
        this.isFormatValid = true;
        this.isDimensionValid = true;

        var file = e.dataTransfer ? e.dataTransfer.files[0] : e.target.files[0];
        var reader = new FileReader();

        console.log(file);

        if (this.accept.indexOf(file.type) === -1) {
            this.isFormatValid = false;
            return;
        }

        reader.onload = this._handleReaderLoaded.bind(this);
        reader.readAsDataURL(file);
    }
    _handleReaderLoaded(e) {
        let reader = e.target;
        var i = new Image();
        i.src = reader.result;
        i.onload = () => {
            if ((this.maxHeight && i.height > this.maxHeight)
                || (this.maxWidth && i.width > this.maxWidth)) {
                this.isDimensionValid = false;
                this.ref.markForCheck();
                return;
            }

            this.value = reader.result;
            this.onModelChange(this.value);
            this.ref.markForCheck();
        };

    }

    get validationMessage(): string {
        if (!this.isFormatValid) {
            return this.invalidFormatMessage;
        }
        if (!this.isDimensionValid) {
            return this.invalidDimensionMessage;
        }
        return '';
    }

    public isValid() {
        return this.isFormatValid && this.isDimensionValid;
    }
}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DirectivesModule,
        ButtonModule
    ],
    declarations: [ImagePicker],
    exports: [ImagePicker]
})

export class ImagePickerModule { }