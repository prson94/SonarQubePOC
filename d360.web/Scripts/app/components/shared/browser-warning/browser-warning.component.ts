import { Component, ChangeDetectionStrategy, OnInit, NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CoreModule } from '../core.module';


@Component({
    selector: 'ig-browser-warning',
    templateUrl: './browser-warning.component.html',
    styleUrls: ['./browser-warning.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class BrowserWarningComponent implements OnInit {
    visible = false;
    dismissKey = 'hasDismissedWarning';
    deprecationDate = 'September 18th 2020';

    alternateBrowsers = [
        { label: 'Chrome (latest stable version)', url: 'https://www.google.com/chrome/'},
        { label: 'Microsoft Edge (latest Chromium based version)', url: 'https://www.microsoft.com/en-us/edge' },
    ];

    constructor() {
    }  

    ngOnInit() {
        this.checkBrowser();
    }

    private checkBrowser() {
        //check for IE11 via feature check to avoid user agent spoofing
        let isBrowserUnsupported = false || !!(document as any).documentMode;

        if (isBrowserUnsupported) {
            let hasDismissedWarning = sessionStorage.getItem(this.dismissKey) || false;
            if (!hasDismissedWarning) {
                this.visible = true;
            }
        }
    }

    protected dismiss() {
        this.visible = false;
        sessionStorage.setItem(this.dismissKey, 'true');
    }

    get currentYear(): string {
        return new Date().getFullYear().toString();
    }
}

@NgModule({
    declarations: [
        BrowserWarningComponent
    ],
    exports: [
        BrowserWarningComponent
    ]
    , imports: [
        CommonModule,
        CoreModule,
    ]
})
export class BrowserWarningModule { }