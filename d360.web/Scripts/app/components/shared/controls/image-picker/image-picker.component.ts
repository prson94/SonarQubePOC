import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, EventEmitter, Output, ViewChild, OnInit } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { DirectivesModule } from "../../../../directives/directives.module";
import { ButtonModule } from "../../../../directives/ig-button-directive";
import { TooltipModule } from "primeng/tooltip";
import { DomSanitizer } from '@angular/platform-browser';

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
    @Input() message = '';
    @Input() imageType = '';
    @Input() allowedExtensions = '';
    @Input() maxHeight: number;
    @Input() maxWidth: number;
    @Input() maxSize: number;

    @Input() chooseFileTooltip: string = 'Choose file';
    @Input() restoreFileTooltip: string = 'Restore the file';

    accept = "";
    value = "";

    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };

    previewHeight: number = 0;
    previewWidth: number = 0;

    invalidFormatMessage: string = 'File type not supported. Please choose a PNG, JPG or GIF image.';
    invalidDimensionMessage: string = 'File exceeds height [or width] limit. Please upload a file that has max file height of [40px].';

    private file: any = {};
    private image: any = {};

    constructor(public ref: ChangeDetectorRef,
        public domSanitizer: DomSanitizer,
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
        this.file = {};
        this.image = {};
        this.onModelChange(this.value);
        this.ref.markForCheck();
    }

    handleInputChange(e) {
        this.value = "";

        this.file = e.dataTransfer ? e.dataTransfer.files[0] : e.target.files[0];
        var reader = new FileReader();

        reader.onload = this._handleReaderLoaded.bind(this);
        reader.readAsDataURL(this.file);
    }

    _handleReaderLoaded(e) {
        let reader = e.target;
        var i = new Image();
        i.src = reader.result;

        i.onload = () => {
            this.image = i;
            this.value = reader.result;
            this.onModelChange(this.value);
            this.ref.markForCheck();
        };
    }

    get errorMessage(): string {
        if (!this.value) {
            return '';
        }

        if (this.accept.indexOf(this.file.type) === -1) {
            return this.invalidFormatMessage;
        }

        if ((this.maxHeight && this.image.height > this.maxHeight)
            || (this.maxWidth && this.image.width > this.maxWidth)) {
            return this.invalidDimensionMessage;
        }

        if (this.file.size > this.maxSize) {
            let sizeStr = "";
            var maxSizeKB = this.maxSize / 1024;
            if (maxSizeKB > 1000) {
                var maxSizeMB = maxSizeKB / 1024;
                sizeStr = parseInt(maxSizeMB.toFixed(0)) + "MB";
            }
            else {
                sizeStr = parseInt(maxSizeKB.toFixed(0)) + "kB";
            }
            return `File exceeds size limit. Please upload a file that has max file size of [${sizeStr}]`;
        }

        return '';
    }

    public isValid() {
        return this.errorMessage.length === 0;
    }
}

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        DirectivesModule,
        ButtonModule,
        TooltipModule

    ],
    declarations: [ImagePicker],
    exports: [ImagePicker]
})

export class ImagePickerModule { }