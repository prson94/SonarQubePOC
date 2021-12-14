import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { DetailRow, DetailField, DetailFieldType, NymType, Category, ComplexLookupType } from '../../../models/object-detail.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetService } from '../../../services/asset.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';
import { SynonymPermission } from '../../../models/artifacts.model';

declare var CurrentResourceID;

@Component({
    selector: 'ig-asset-type-detail',
    templateUrl: './asset-type-detail.component.html',
    providers: [ObjectDetailService, AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})


export class AssetTypeDetailComponent implements OnChanges {
    @Input() uid: string;
    @Input() paddingLeft: string;

    isLoading: boolean = false;

    constructor(
        protected messagesService: MessagesObservableService,
        private assetService: AssetService,
        private cdRef: ChangeDetectorRef) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'uid') {
                this.load();
            }
        }
    }

    public load(): void {
        console.log(this.uid);
    }

    open(isNewTab: boolean) {
        console.log("oopen link");
    }
}
