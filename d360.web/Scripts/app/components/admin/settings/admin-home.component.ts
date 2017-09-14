import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CompanySettings, CompanyImage } from '../../../models/settings.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-home',
    templateUrl: './admin-home.component.html'
})

export class AdminHomeComponent implements OnInit, OnChanges {
    @Input() companySettings: CompanySettings;
    @Output() companySettingsChange = new EventEmitter();
    @Input() homePageImage: CompanyImage;
    @Output() homePageImageChange = new EventEmitter();
    constructor() {
    }

    ngOnInit() {
    }

    ngOnChanges(changes: SimpleChanges) {}
    

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

        this.homePageImage.file = files[0];
        this.homePageImage.setDataUrl();

        this.homePageImageChange.emit(this.homePageImage);
    }

}
