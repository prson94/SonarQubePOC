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
            case 'breadcrumbLinkColor':
                return this.breadcrumbLinkColorHtmlTemplate + `Breadcrumbs help you know where you are in the D360 Govern. Use a color that is clearly visible on the navigation bar.`;
            case 'buttonBackColor':
                return this.buttonBackColorHtmlTemplate + `This button is displayed on the navigation bar. Make sure it is clearly visible against the background of the navigation bar.`;
            case 'navbarBackColor':
                return this.navbarHtmlTemplate + `This is the main side navigation in the D360 Govern, so make sure its content is clearly visible.`;
            case 'navbarBackColorSelected':
                return this.navbarHtmlTemplate + `This is the color option for the selected item in the side menu, so make sure its content is clearly visible.`;
            case 'homeBackground':
                return `The chosen image will be placed at the top of the home page and stretched horizontally to fit.`;
            case 'primaryButtonBackColor':
                return this.primaryButtonBackColorHtmlTemplate + `The text color is automatically adjusted to the component color to comply with accessibility rules.`;
            case 'backColor':
                return `The background color should be as neutral as possible so that all elements in the D360 Govern are clearly visible.`;
            case 'tabLinkColor':
                return this.tabLinkColorHtmlTemplate + `Remember that clickable elements should be in a color that is easily visible and suggests clickability.`;
            case 'tableHeaderBackColor':
                return this.tableHeaderBackColorHtmlTemplate + `Data table elements should be as neutral as possible so that reading the data is not disturbed by flashy colors.`;
            case 'tableRowBackColor':
                return this.tableHeaderBackColorHtmlTemplate + `Data table elements should be as neutral as possible so that reading the data is not disturbed by flashy colors.`;
            default:
                return "";
        }
    }

    get headerBackColorHtmlTemplate(): string {
        var color = this.formGroup.get('headerBackColor').value;
        return `<div class='headerBackColor-template'>
<i class="fa fa-shopping-cart" style="background-color:${color};"></i>
<i class="fa fa-star" style="background-color:${color};"></i>
<i class="fa fa-home selected" style="background-color:${color};"></i>
<i class="fa fa-question-circle" style="background-color:${color};"></i>
</div>`;
    }

    get breadcrumbLinkColorHtmlTemplate(): string {
        var headerColor = this.formGroup.get('headerBackColor').value;
        var color = this.formGroup.get('breadcrumbLinkColor').value;

        return `<div class="breadcrumbLinkColor-template" style="background-color:${headerColor}">
        <div style="color:${color};">Administration</div> <i class="fa fa-angle-right"></i> <div class="selected">Branding</div>
    </div>`;
    }

    get buttonBackColorHtmlTemplate(): string {
        var color = this.formGroup.get('buttonBackColor').value;

        return `<div style="background-color:${color};" class="buttonBackColor-template">Take Action</div>`;
    }

    get tabLinkColorHtmlTemplate(): string {
        var color = this.formGroup.get('tabLinkColor').value;

        return `<div class="tabLinkColor-template">
        <div class="display-flex">
            <div class="link-template selected">Assets</div>
            <div class="link-template" style="color:${color};">Workflow</div>
        </div>

        <div class="link-template-diagram display-flex">
            <i class="fa fa-plus-square" style="color:${color};"></i>
            <div class="text" style="color:${color};">Diagram Badge</div>
            <div class="badge" style="background-color:${color};">1</div>
        </div>
    </div>`;
    }

    get primaryButtonBackColorHtmlTemplate(): string {
        var color = this.formGroup.get('primaryButtonBackColor').value;

        return `<div style="background-color:${color};" class="buttonBackColor-template">Save Changes</div>`;
    }

    get tableHeaderBackColorHtmlTemplate(): string {
        var tableHeaderColor = this.formGroup.get('tableHeaderBackColor').value;
        var selectedRowColor = this.formGroup.get('tableRowBackColor').value;

        return `<div class="table-template">
            <div class="header-template" style="background-color:${tableHeaderColor};">
                <div class="col">Name <i class="fa fa-fw fa-sort"></i></div>
                <div class="col">Status <i class="fa fa-fw fa-sort"></i></div>
            </div>
            <div class="row-template">
                <div class="col">Asset 1</div>
                <div class="col"><div class="status green"></div> Certified</div>
            </div>
            <div class="row-template" style="background-color:${selectedRowColor};">
                <div class="col">Asset 1</div>
                <div class="col"><div class="status grey"></div> Draft</div>
            </div>
        </div>`;
    }

    get navbarHtmlTemplate(): string {
        var bgColor = this.formGroup.get('navbarBackColor').value;
        var selectedBgColor = this.formGroup.get('navbarBackColorSelected').value;

        return `<div class="navbarBackColor-template" style="background-color:${bgColor};">
        <div class="menu-item-template">
            <i class="fa fa-home"></i>
            <div class="text flex-grow">Home</div>
        </div>
        <div class="menu-item-template">
            <i class="fa fa-star"></i>
            <div class="text flex-grow">Favorites</div>
            <i class="fa fa-angle-right"></i>
        </div>
        <div class="menu-item-template" style="background-color:${selectedBgColor}">
            <i class="fa fa-star"></i>
            <div class="text flex-grow">Favorites</div>
            <i class="fa fa-angle-right"></i>
        </div>
        <div class="menu-item-template">
            <i class="fa fa-book"></i>
            <div class="text flex-grow">Business Assets</div>
            <i class="fa fa-angle-right"></i>
        </div>
    </div>`;
    }
}