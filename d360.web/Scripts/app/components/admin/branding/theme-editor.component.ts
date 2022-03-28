import { ChangeDetectionStrategy, Component, Input, ViewEncapsulation } from '@angular/core';
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

export class ThemeEditorComponent {
    @Input() uid: string = '';
    @Input() isVisible: boolean = false;

    data: Theme;

    savingInProgress: boolean = false;
    formGroup: FormGroup = null;

    constructor(private fb: FormBuilder,
        private brandingService: BrandingService
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
            tableRowBackColor: ['']
        });
    }

    save() {
        var theme = new Theme();
        var properties = Object.keys(this.formGroup.controls);

        properties.forEach((p) => {
            var f = this.formGroup.controls[p];
            theme[p] = this.formGroup.get(p).value;
        });
        console.log(theme);

        this.brandingService.saveTheme(theme).subscribe((res) => {
            console.log(res);
        })
    }
}