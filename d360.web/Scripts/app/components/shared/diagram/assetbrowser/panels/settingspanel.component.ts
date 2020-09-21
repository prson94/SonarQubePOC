import * as _ from 'lodash';
import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, Output, EventEmitter } from '@angular/core';
import { AssetBrowserFilterModel, AssetBrowserFilterChangeEvent, AssetBrowserFilterChangeEventType, DiagramType } from '../../../../../models/lineage.model';

import { MessagesObservableService } from '../../../../../services/messages-observable.service';

@Component({
    selector: 'd3s-assetbrowser-settingspanel',
    templateUrl: './settingspanel.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserSettingsPanelComponent implements AfterViewInit {
    @Input() allowAncestry: boolean; 
    @Input() current: AssetBrowserFilterModel;
    @Output() apply: EventEmitter<AssetBrowserFilterChangeEvent> = new EventEmitter();

    constructor(
        protected messagesService: MessagesObservableService,
        private cdRef: ChangeDetectorRef
    ) {
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    allBadgesChange(): void {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.AllBadges, Model: this.current });
    }

    ancestorBadgesChange(): void {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.AncestorBadges, Model: this.current });
    }

    iconsChange(): void {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.Icons, Model: this.current });
    }

    scoreChange(): void {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.Scores, Model: this.current });
    }
} 