import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BrandingService, Theme } from '../../../services/branding.service';

@Component({
    selector: "theme-editor",
    templateUrl: "theme-editor.component.html",
    encapsulation: ViewEncapsulation.None,
    providers: [BrandingService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styleUrls: ["./theme-editor.component.less"]
})

export class ThemeEditorComponent implements OnChanges {
    @Input() uid: string = '';
    @Input() isVisible: boolean = false;
    @Input() theme: Theme;

    @Output() onSave = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    data: Theme;

    savingInProgress: boolean = false;
    formGroup: FormGroup = null;

    isCurrentTheme: boolean = false;

    constructor(private fb: FormBuilder,
        private brandingService: BrandingService,
        private cdRef: ChangeDetectorRef
    ) {
        this.formGroup = this.fb.group({
            name: [null, { validators: [Validators.required] }],
            headerLogo: [''],
            icon: [''],
            headerBackColor: [''],
            breadcrumbLinkColor: [''],
            buttonBackColor: [''],
            homeBackground: [''],
            primaryButtonBackColor: [''],
            backColor: [''],
            tabLinkColor: [''],
            tableHeaderBackColor: [''],
            tableRowBackColor: [''],
            navbarBackColor: [''],
            navbarBackColorSelected: ['']
        });

        this.setForm();
    }

    ngOnChanges(changes: SimpleChanges) {
        if ((changes.uid && changes.uid.currentValue !== changes.uid.previousValue)
            || (changes.theme && changes.theme.currentValue !== changes.theme.previousValue)
        ) {
            this.setForm();
        }
    }

    setForm() {
        var _th = this.theme ? this.theme : new Theme(true);
        this.isCurrentTheme = this.theme ? this.theme.isCurrent : false;
        var properties = Object.keys(this.formGroup.controls);

        properties.forEach((p) => {
            var valObj = {};
            valObj[p] = _th[p];
            this.formGroup.patchValue(valObj);
        });
    }

    save() {
        this.savingInProgress = true;
        var _theme = new Theme();
        if (this.theme?.uid) {
            _theme.uid = this.theme.uid;
        }
        var properties = Object.keys(this.formGroup.controls);

        properties.forEach((p) => {
            _theme[p] = this.formGroup.get(p).value;
        });

        this.brandingService.saveTheme(_theme).subscribe((res) => {
            if (res) {
                this.onSave.emit();
                this.setForm();
            }
            this.savingInProgress = false;
            this.cdRef.markForCheck();
        });
    }

    cancel() {
        this.isVisible = false;
        this.setForm();
        this.onCancel.emit();
    }

    get themeEditorHeight() {
        return window.innerHeight - 280;
    }

    getTooltip(prop: string): string {

        switch (prop) {
            case 'headerBackColor':
                return this.headerBackColorHtmlTemplate + `When choosing the color of the top navigation bar, bear in mind that it contains several important elements e.g. logo, search field, icons.`;
            default:
                return "";
        }
    }

    get headerBackColorHtmlTemplate(): string {
        var color = this.formGroup.get('headerBackColor').value;
        console.log(color);
        
        return `<div class='headerBackColor-template'>
<i class="fa fa-shopping-cart" style="background-color:${color};"></i>
<i class="fa fa-star" style="background-color:${color};"></i>
<i class="fa fa-home selected" style="background-color:${color};"></i>
<i class="fa fa-question-circle" style="background-color:${color};"></i>
</div>`;
    }
}