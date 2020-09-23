import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CompanySettings, CompanyImage } from '../../../models/settings.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';

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
                this.messagesService.showError('File too large.', `The specified image is too large.  Please resave as a smaller image to continue.  Specified file is ${(files[0].size).toLocaleString()} bytes in size which is greater than the max allowed size of ${(1024 * 1000).toLocaleString()} bytes.`);
                return;
            }
        }

        this.homePageImage.file = files[0];

        this.homePageImage.setDataUrl();

        this.homePageImageChange.emit(this.homePageImage);
    }
}
