import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-locale',
    templateUrl: './gallery.locale.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryLocaleComponent implements OnInit {
    currentLanguage: string = '';

    translatedText: string = $localize`This text comes from .TS file.`;
    tooltipText: string = $localize`Localized tooltip text which must be set in ts file before binding to components attribute`;

    constructor(private langService: LanguageService) {

    }

    ngOnInit(): void {

    }
}