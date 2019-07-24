import { Component, Input, Output, EventEmitter } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { CompanySettings, CompanyImage } from '../../../models/settings.model';
import { SiteNav } from '../../../models/site-menu.model';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';

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
                    You may upload a custom logo for your company.  The following formats are accepted: .GIF, .JPG, .PNG.<br />  
                    The image will be scaled to 40px high as necessary.
                </div>
            </div>
            <div class="col s6">
                <div class="directions">
                    You may upload a custom web icon (.ICO file) for your company.
                </div>
            </div>
        </div>
        <div class="row">
            <div class="col s6">
                <div *ngIf="companySettings.CurrentCompanyLogoPath">
                    Current Logo: <br />
                    <img [src]="companySettings.CurrentCompanyLogoPath" class="company-logo-canvas" />
                </div>
                &nbsp;
            </div>
            <div class="col s6">
                <div *ngIf="companySettings.CurrentCompanyIconPath">
                    Current Icon: <br />
                    <img [src]="companySettings.CurrentCompanyIconPath" />
                </div>
            </div>
        </div>
        <div class="row" *ngIf="companySettings.CurrentCompanyLogoPath || companySettings.CurrentCompanyIconPath">
            <div class="col s6">
                <input type="checkbox" [ngModel]="companySettings.SetLogoToDefault" (ngModelChange)="companySettings.SetLogoToDefault = $event; companySettingsChange.emit(companySettings)" /> Reset to default
            </div>
            <div class="col s6">
                <input type="checkbox" [ngModel]="companySettings.SetIconToDefault" (ngModelChange)="companySettings.SetIconToDefault = $event; companySettingsChange.emit(companySettings)" /> Reset to default
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
        private messagesService: MessagesObservableService
    ) {
        super(headerBreadcrumbService, titleService);
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

        this.companyIcon.file = files[0];
        this.companyIcon.setDataUrl();

        this.companyIconChange.emit(this.companyIcon);
    }

    modelChanged() {
        this.companySettingsChange.emit(this.companySettings);
    }
}
