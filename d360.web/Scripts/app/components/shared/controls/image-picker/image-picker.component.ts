import { Component, NgModule, ViewEncapsulation, ChangeDetectionStrategy, Input, ChangeDetectorRef, forwardRef, ElementRef, EventEmitter, Output, ViewChild, OnInit } from "@angular/core";
import { FormsModule, ControlValueAccessor, ReactiveFormsModule, NG_VALUE_ACCESSOR, Validator, AbstractControl, ValidationErrors } from "@angular/forms";
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
export class ImagePicker implements ControlValueAccessor, OnInit, Validator {
    @Input() message = '';
    @Input() imageType = '';
    @Input() allowedExtensions = 'image/png,image/gif,image/jpg,image/jpeg';
    @Input() maxHeight: number;
    @Input() maxWidth: number;
    @Input() maxSize: number;

    @Input() chooseFileTooltip: string = 'Choose file';
    @Input() restoreFileTooltip: string = 'Restore the file';

    value = "";

    onModelChange: Function = () => { };
    onModelTouched: Function = () => { };
    onValidationChange: Function = () => { };

    hasError: boolean = false;
    validationMessage: string = "";

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
        this.setPreviewDimensions();
        this.setInvalidMessages();
    }

    setPreviewDimensions() {
        switch (this.imageType) {
            case 'ICO':
                this.previewHeight = 40;
                this.previewWidth = 40;
                this.allowedExtensions = "image/ico,image/x-icon";
                this.invalidFormatMessage = 'File type not supported. Please choose a ICO image.';
                break;
            case 'LOGO':
                this.previewHeight = 40;
                this.previewWidth = 168;
                break;
            default:
                this.previewHeight = 96;
                this.previewWidth = 168;
                break;
        }
    }

    setInvalidMessages() {
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

    registerOnValidatorChange(fn: () => void): void {
        this.onValidationChange = fn;
    }

    clearValue() {
        this.value = "";
        this.file = {};
        this.image = {};
        this.validate();
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
            this.validate();
            this.ref.markForCheck();
        };
    }

    defaultValidators = [
        {
            type: 'invalid-format',
            validator: () => {
                if (this.allowedExtensions.indexOf(this.file.type) === -1) {
                    return this.invalidFormatMessage;
                }
            }
        },
        {
            type: 'invalid-dimensions',
            validator: () => {
                if ((this.maxHeight && this.image.height > this.maxHeight)
                    || (this.maxWidth && this.image.width > this.maxWidth)) {
                    return this.invalidDimensionMessage;
                }
            }
        },
        {
            type: 'invalid-size',
            validator: () => {
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
            }
        }
    ];

    validate(control: AbstractControl = null): ValidationErrors {
        const error = this.getValidationError(control ? control.value : this.value);
        this.hasError = (error != null);
        this.validationMessage = error ? error[Object.keys(error)[0]] : '';
        return error;
    }

    getValidationError(value: string) {
        if (value == null || value.trim() === '') {
            return null;
        }

        for (let i = 0; i < this.defaultValidators.length; i++) {
            var errorMessage = this.defaultValidators[i].validator();
            if (errorMessage) {
                var err = {};
                err[this.defaultValidators[i].type] = errorMessage;
                return err;
            }
        }

        return null;
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