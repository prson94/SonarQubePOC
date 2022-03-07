import { ChangeDetectionStrategy, Component, Input, OnChanges, OnDestroy, SimpleChange } from '@angular/core';
import { Subscription } from 'rxjs';
import { LinkClickInterceptor } from '../../../services/href-click-service';


@Component({
    selector: 'asset-preview',
    templateUrl: './asset-preview.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetPreviewComponent implements OnChanges, OnDestroy {
    @Input() assetUid: string = '';
    @Input() assetType: string = '';

    selectedAsset: any;
    selectedReferenceItem: any;
    selectedTag: any;

    hrefSub: Subscription;

    constructor(
        private linkClickInterceptor: LinkClickInterceptor
    ) {
        this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
            this.linkClickInterceptor.handleEvent(this, ev);
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if ((p === 'assetUid' || p === 'assetType') && this.assetType && this.assetUid) {
                this.selectedAsset = { uid: this.assetUid, type: this.assetType };
            }
        }
    }

    ngOnDestroy() {
        if (this.hrefSub) {
            this.hrefSub.unsubscribe();
        }
    }
}
