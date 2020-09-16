import { AfterViewInit, Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { AssetBrowserAlert, AssetBrowserAlertRequest } from '../../../../../models/lineage.model';

import { BrowserService } from '../../../../../services/browser.service';
import { PermissionsService } from '../../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-alertpanel',
    templateUrl: './alertpanel.component.html',
    providers: [BrowserService, PermissionsService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserAlertPanelComponent implements OnInit, AfterViewInit, OnChanges {
    @Input() assets: string[] = [];
    @Output() openDetail: EventEmitter<AssetBrowserAlert> = new EventEmitter();

    alerts: AssetBrowserAlert[] = [];
    loading: boolean = false;

    constructor(
        private browserService: BrowserService,
        protected permissionsService: PermissionsService,
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
    }

    public ngOnInit() {
        this.reloadAlerts();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["assets"]) {
            this.reloadAlerts();
        }
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    private openAssetDetail(alert: AssetBrowserAlert) {
        this.openDetail.emit(alert);
    }

    private openInNewTab(alert: AssetBrowserAlert) {
        window.open(`/asset/${alert.asset.uid}`, "_blank");
    }

    private reloadAlerts() {
        if (this.assets.length > 0) {
            this.loading = true;

            let model: AssetBrowserAlertRequest = new AssetBrowserAlertRequest();

            this.assets.forEach(a => {
                model.assets.push({ uid: a });
            });
            this.browserService.getAlertsByAsset(model).subscribe(alerts => {
                if (alerts) {
                    this.alerts = alerts;
                }
                else {
                    this.alerts = [];
                }
                this.loading = false;
                this.cdRef.markForCheck();
            });
        }
    }
} 