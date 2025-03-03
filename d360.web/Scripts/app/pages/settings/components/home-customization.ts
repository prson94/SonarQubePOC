import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CompanyImage, CompanySettings } from '../../../models/settings.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { IgCheckboxModule } from '../../../directives/ig-checkbox-directive';
import { CheckboxModule } from 'primeng/checkbox';
import { ColorPickerModule } from 'primeng/colorpicker';
import { ShortcutModule } from '../../../components/shared/shortcuts/shortcut.module';

@Component({
	selector: 'home-customization',
	templateUrl: './home-customization.html',
	standalone: true,
	imports: [CheckboxModule, ColorPickerModule, FormsModule, IgCheckboxModule, ShortcutModule]
})
export class HomeCustomization  {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();
    @Input() homePageImage: CompanyImage;
    @Output() homePageImageChange = new EventEmitter();
    constructor(private messagesService: MessagesObservableService) { }
    
    onFileChange(event): void {
        if (this.homePageImage == null)
            {this.homePageImage = new CompanyImage();}

        if (!event) {
            this.homePageImage.file = null;
            this.homePageImage.setDataUrl();
            return;
        }

        if (this.companySettings.ClearHomePageBackgroundImage) {
            this.companySettings.ClearHomePageBackgroundImage = false;
            this.companySettingsChange.emit(this.companySettings);
        }

        const target = event.target || event.srcElement;
        const files = target.files;

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
