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

    txtEn: string = $localize`English`;
    txtDe: string = $localize`German`;
    txtFr: string = $localize`French`;

    ngOnInit(): void {

    }

    changeLang(newLocale) {
        var newLang = this.getLang(newLocale);
        let langConsts: string[] = ['/fr/', '/de/', '/en-us/'];
        langConsts.forEach((lang) => {
            if (window.location.href.indexOf(lang) !== -1) {
                var length = lang.length;
                var idx = window.location.href.indexOf(lang);
                var newUrl = window.location.href.substring(0, idx) + newLang + window.location.href.substring(idx + length, window.location.href.length);
                window.location.href = newUrl;
            }
        })
    }

    getLang(value: string) {
        return `\\${value}\\`;
    }
}