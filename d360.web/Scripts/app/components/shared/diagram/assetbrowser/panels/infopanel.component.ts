import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { AssetBrowserDiagramAsset } from '../../../../../models/lineage.model';
import { BrowserService } from '../../../../../services/browser.service';
import { PermissionsService } from '../../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-infopanel',
    templateUrl: './infopanel.component.html',
    providers: [BrowserService, PermissionsService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserInfoPanelComponent implements AfterViewInit {
    @Input() asset: AssetBrowserDiagramAsset;

    constructor(
        protected permissionsService: PermissionsService,
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
            return "Error";
        }
    }

    getLinkHtml(value: string): string {
        if (value == null || value.length === 0) {
            return "";
        }
        if (value.indexOf('|') == -1) {
            return `<a href="${value}">${value}</a>`
        }
        else {
            return `<a href="${value.split('|')[1]}">${value.split('|')[0]}</a>`
        }
    }
} 