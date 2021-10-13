import { Component, Input, Output, EventEmitter } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, CompanyImage } from '../../../models/settings.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-image-upload',
    template: `
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <header>
            Custom Images
            <div class="TileTools"></div>
        </header>
        <div class="row">
            <div class="col s6"><div class="FieldName">Company Logo</div></div>
            <div class="col s6"><div class="FieldName">Company Web Icon</div></div>
        </div>
        <div class="row">
            <div class="col s6">
                <div class="directions">
                    You may upload a custom logo for your company.  The following formats are accepted: .GIF, .JPG, .PNG.  The image will be scaled to 40px high as necessary and up to a maximum file size of 1MB.
                </div>
            </div>
            <div class="col s6">
                <div class="directions">
                    You may upload a custom web icon (.ICO file) for your company, up to a maximum file size of 1MB.
                </div>
            </div>
        </div>
        <div class="row">
            <div class="col s6">
                <div *ngIf="companySettings.CurrentLogoPath">
                    Current Logo: <br />
                    <img [src]="companySettings.CurrentLogoPath" class="company-logo-canvas" />
                </div>
                &nbsp;
            </div>
            <div class="col s6">
                <div *ngIf="companySettings.CurrentIconPath">
                    Current Icon: <br />
                    <img [src]="companySettings.CurrentIconPath" />
                </div>
            </div>
        </div>
        <div class="row" *ngIf="companySettings.CurrentLogoPath !== companySettings.DefaultLogoPath || companySettings.CurrentIconPath !== companySettings.DefaultIconPath">
            <div class="col s6">
                <p-checkbox igCheckbox
                            [(ngModel)]="companySettings.SetLogoToDefault"
                            label="Reset to default"
                            (ngModelChange)="companySettings.SetLogoToDefault = $event; companySettingsChange.emit(companySettings)"
                            binary="true">
                </p-checkbox>
            </div>
            <div class="col s6">
                <p-checkbox igCheckbox
                            [(ngModel)]="companySettings.SetIconToDefault"
                            label="Reset to default"
                            (ngModelChange)="companySettings.SetIconToDefault = $event; companySettingsChange.emit(companySettings)"
                            binary="true">
                </p-checkbox>
            </div>
        </div>
        <div class="row">
            <div class="col s6">
                <input #logoUpload type="file" (change)="onLogoFileChange($event)" accept="image/gif,image/jpeg,image/png" />
                <span *ngIf="companyLogo?.isLoading"><i class="fa fa-spinner fa-spin"></i></span>
                <a *ngIf="logoUpload.files.length > 0" (click)="logoUpload.value = null; onLogoFileChange(null);" class="remove"><i class="fa fa-remove"></i></a>
            </div>
            <div class="col s6">
                <input #iconUpload type="file" (change)="onIconFileChange($event)" accept="image/vnd.microsoft.icon,image/x-icon" />
                <span *ngIf="companyIcon?.isLoading"><i class="fa fa-spinner fa-spin"></i></span>
                <a *ngIf="iconUpload.files.length > 0" (click)="iconUpload.value = null; onIconFileChange(null);" class="remove"><i class="fa fa-remove"></i></a>
            </div>
        </div>
    </div>
`,
    styles: [
        `
        .remove {
            cursor: pointer; 
            color: maroon; 
            font-size: 1.5em;
            vertical-align: middle;
        }
        input[type=text] {
            width: 90%;
            height:25px;
        }
        `
    ]
})

export class AdminImageUploadComponent extends AdminBaseComponent {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();
    @Input() companyLogo: CompanyImage;
    @Input() companyIcon: CompanyImage;
    @Output() companyLogoChange = new EventEmitter();
    @Output() companyIconChange = new EventEmitter();

    constructor(
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(headerBreadcrumbService, titleService, settingsService);
    }


    onLogoFileChange(event): void {
        if (this.companyLogo == null)
            this.companyLogo = new CompanyImage();

        if (!event) {
            this.companyLogo.file = null;
            this.companyLogo.setDataUrl();
            return;
        }

        let target = event.target || event.srcElement;
        let files = target.files;

        if (files[0] != null)
        {
            if (files[0].size > (1024 * 1024)) {
                this.messagesService.showError('File too large.', `Company logo image upload failed - the file is too large. Please choose an image file (ideally in JPG format due to smaller file size) no bigger than 1MB. `);
                target.value = null;
                return;
            }
        }

        this.companyLogo.file = files[0];

        this.companyLogo.setDataUrl();

        this.companyLogoChange.emit(this.companyLogo);
    }

    onIconFileChange(event): void {
        if (this.companyIcon == null)
            this.companyIcon = new CompanyImage();
        if (!event) {
            this.companyIcon.file = null;
            this.companyIcon.setDataUrl();
            return;
        }

        let target = event.target || event.srcElement;
        let files = target.files;

        if (files[0] != null) {
            if (files[0].size > (1024 * 1024)) {
                this.messagesService.showError('File too large.', `Company icon image upload failed - the file is too large. Please choose an image file no bigger than 1MB. `);
                target.value = null;
                return;
            }
        }

        this.companyIcon.file = files[0];

        this.companyIcon.setDataUrl();

        this.companyIconChange.emit(this.companyIcon);
    }

    modelChanged() {
        this.companySettingsChange.emit(this.companySettings);
    }
}
