import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { AssetBrowserDiagramAsset } from '../../../../../models/lineage.model';

import { BrowserService } from '../../../../../services/browser.service';
import { PermissionsService } from '../../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-ownerpanel',
    templateUrl: './ownerpanel.component.html',
    providers: [BrowserService, PermissionsService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserOwnerPanelComponent implements AfterViewInit {
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

    private ownerRowClass(icon: string) {
        return "fa " + icon;
    }
} 