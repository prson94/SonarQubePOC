import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import '@angular/localize/init';


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
    minutes = 0;
    gender = 'female';

    translatedText: string = $localize`:attributes set in .ts file|Random translated text@@ts-translation-id:This text comes from .TS file.`;
    tooltipText: string = $localize`:attributes set in .ts file|Localization Tooltip@@ts-translation-id-toolptip:Localized tooltip text which must be set in ts file before binding to components attribute`;

    ngOnInit(): void {
    }

    inc(i: number) {
        this.minutes = Math.min(5, Math.max(0, this.minutes + i));
    }
    male() { this.gender = 'male'; }
    female() { this.gender = 'female'; }
    other() { this.gender = 'other'; }
}