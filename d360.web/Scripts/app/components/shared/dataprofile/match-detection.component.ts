import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { LazyLoadEvent } from 'primeng/api';
import { AssetService } from '../../../services/asset.service';
import { DataProfileService } from '../../../services/dataprofile.service';

@Component({
    selector: 'match-detection',
    templateUrl: './match-detection.component.html',
    styleUrls: ['match-detection.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AssetService, DataProfileService]
})

export class MatchDetectionComponent implements OnChanges {
    @Input() isVisible: boolean = false;
    @Input() assetUid: string = '';

    @Output() onClose = new EventEmitter();

    private assetPath: string = '';

    duplicatesData: any[] = [];
    duplicatesDataTotalCount: number = 0;
    duplicatesSelection: any[] = [];
    duplicatesDataLoading: boolean = true;

    similiarData: any[] = [];
    similiarDataTotalCount: number = 0;
    similiarSelection: any[] = [];
    similiarDataLoading: boolean = true;

    constructor(
        private assetService: AssetService,
        private dataProfileService: DataProfileService,
        private cdRef: ChangeDetectorRef
    ) {

    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.assetUid && changes.assetUid.currentValue !== changes.assetUid.previousValue) {
            this.loadData();
        }
    }

    private loadData() {
        this.assetService.getAsset(this.assetUid)
            .subscribe((res) => {
                this.assetPath = res.Path;
            });
    }

    lazyLoad(e: LazyLoadEvent, type: string) {
        if (type === "Data") {
            this.lazyLoadDuplicates(e);
        }
        else {
            this.lazyLoadSimiliar(e);
        }

    }

    lazyLoadDuplicates(e: LazyLoadEvent) {
        this.duplicatesDataLoading = true;
        console.log(e);
        this.dataProfileService.getMatchesByMatchType(this.assetUid, "Data",1,10)
            .subscribe((res) => {
                this.duplicatesData = res.items;
                this.duplicatesDataTotalCount = +res.total;
                this.duplicatesDataLoading = false;
                this.cdRef.markForCheck();
            });
    }

    lazyLoadSimiliar(e: LazyLoadEvent) {
        this.similiarDataLoading = true;

        this.dataProfileService.getMatchesByMatchType(this.assetUid, "Structure", 1, 10)
            .subscribe((res) => {
                this.similiarData = res.items;
                this.similiarDataTotalCount = +res.total;
                this.similiarDataLoading = false;
                this.cdRef.markForCheck();
            });
    }
}