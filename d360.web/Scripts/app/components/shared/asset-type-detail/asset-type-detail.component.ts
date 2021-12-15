import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, ChangeDetectionStrategy, OnDestroy, ViewEncapsulation } from '@angular/core';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetService } from '../../../services/asset.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetTypeApiModel } from '../../../models/asset.model';
import { Subscription } from 'rxjs';

@Component({
    selector: 'ig-asset-type-detail',
    templateUrl: './asset-type-detail.component.html',
    providers: [ObjectDetailService, AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: ['.p-datatable-wrapper { overflow:auto; } .p-datatable-wrapper table { table-layout: unset !important; }'],
    encapsulation: ViewEncapsulation.None
})


export class AssetTypeDetailComponent implements OnChanges, OnDestroy {
    @Input() uid: string;
    @Input() paddingLeft: string;

    isLoading: boolean = false;
    assetType: AssetTypeApiModel;

    loadSub: Subscription;
    tab: string = 'items';

    constructor(
        protected messagesService: MessagesObservableService,
        private assetService: AssetService,
        private assetTypeService: AssetTypeService,
        private cdRef: ChangeDetectorRef) { }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p === 'uid') {
                this.load();
            }
        }
    }

    ngOnDestroy() {
        if (this.loadSub) {
            this.loadSub.unsubscribe();
        }
    }

    public load(): void {
        this.isLoading = true;
        if (this.loadSub) {
            this.loadSub.unsubscribe();
        }
        this.loadSub = this.assetTypeService.GetAssetTypeByUid(this.uid).subscribe((res) => {
            this.assetType = res;
            this.isLoading = false;
            this.cdRef.markForCheck();
            console.log(this.assetType);
        });
    }

    open(isNewTab: boolean = false) {
        console.log("oopen link");
    }

    clickTab(key: string) {
        this.tab = key;
    }
}
