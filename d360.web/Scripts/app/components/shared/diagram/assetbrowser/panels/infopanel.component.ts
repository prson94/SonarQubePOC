import * as _ from 'lodash';
import { AfterViewInit, Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
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
export class AssetBrowserInfoPanelComponent implements OnInit, AfterViewInit {
    @Input() asset: AssetBrowserDiagramAsset;

    constructor(
        protected permissionsService: PermissionsService,
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
    }

    public ngOnInit() {
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    private GetJSON(value: string) {
        try {
            return JSON.parse(value);
        } catch (err) {
            return "Error";
        }
    }
} 