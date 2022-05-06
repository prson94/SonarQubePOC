import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CompanySettings, CompanyImage } from '../../../models/settings.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import '@angular/localize/init';

@Component({
    selector: 'd3s-admin-home',
    templateUrl: './admin-home.component.html'
})

export class AdminHomeComponent  {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();
    @Input() homePageImage: CompanyImage;
    @Output() homePageImageChange = new EventEmitter();
    constructor(private messagesService: MessagesObservableService) { }
    
    onFileChange(event): void {
        if (this.homePageImage == null)
            this.homePageImage = new CompanyImage();

        if (!event) {
            this.homePageImage.file = null;
            this.homePageImage.setDataUrl();
            return;
        }

        if (this.companySettings.ClearHomePageBackgroundImage) {
            this.companySettings.ClearHomePageBackgroundImage = false;
            this.companySettingsChange.emit(this.companySettings);
        }

        let target = event.target || event.srcElement;
        let files = target.files;

        if (files[0] != null) {
            if (files[0].size > (1024 * 1000)) {
                this.messagesService.showError($localize`File too large.`, $localize`Background image upload failed - the file is too large. Please choose an image file (ideally in JPG format due to smaller file size) no bigger than 1MB. `);
                return;
            }
        }

        this.homePageImage.file = files[0];

        this.homePageImage.setDataUrl();

        this.homePageImageChange.emit(this.homePageImage);
    }
}
