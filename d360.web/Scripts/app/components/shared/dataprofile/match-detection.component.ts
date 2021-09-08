import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { LazyLoadEvent } from 'primeng/api';
import { AssetService } from '../../../services/asset.service';
import { DataProfileService } from '../../../services/dataprofile.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'match-detection',
    templateUrl: './match-detection.component.html',
    styleUrls: ['match-detection.less'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [AssetService, DataProfileService]
})

export class MatchDetectionComponent extends BaseComponent implements OnChanges {
    @Input() isVisible: boolean = false;
    @Input() showDuplicates: boolean = true;
    @Input() showSimilar: boolean = true;

    @Input() assetUid: string = '';
    @Input() matchType: string = '';
    @Output() onClose = new EventEmitter();

    assetPathText: string = '';

    duplicatesData: any[] = [];
    duplicatesDataTotalCount: number = 0;
    duplicatesSelection: any;
    duplicatesDataLoading: boolean = true;
    duplicatesSimpleFilter: string = '';

    similarData: any[] = [];
    similarDataTotalCount: number = 0;
    similarSelection: any;
    similarDataLoading: boolean = true;
    similarSimpleFilter: string = '';

    menuItems = [
        {
            title: 'Open'
        },
        {
            title: 'Open in New Tab'
        }
    ]

    constructor(
        private assetService: AssetService,
        private dataProfileService: DataProfileService,
        private cdRef: ChangeDetectorRef,
        private router: Router
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        if (changes.assetUid && changes.assetUid.currentValue !== changes.assetUid.previousValue) {
            this.loadData();
        }
    }

    private loadData() {
        this.assetService.getAsset(this.assetUid)
            .subscribe((res) => {
                var path = res.Path as string;

                this.assetPathText = "Showing matches for " + path.split("].[").join(`&nbsp;&nbsp;<i class='fa fa-angle-right'></i>&nbsp;&nbsp;`)
                    .replace("[", "").replace("]", "");
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

        let pageSize: number = e.rows;
        let pageNumber: number = (e.first / e.rows) + 1;

        localStorage.setItem("duplicate-rows", e.rows.toString());

        this.dataProfileService.getMatchesByMatchType(this.assetUid, "Data", pageNumber, pageSize, this.duplicatesSimpleFilter)
            .subscribe((res) => {
                this.duplicatesDataTotalCount = +res.total;
                this.duplicatesData = res.items;
                this.duplicatesDataLoading = false;
                this.cdRef.markForCheck();
            });
    }

    lazyLoadSimiliar(e: LazyLoadEvent) {
        this.similarDataLoading = true;

        let pageSize: number = e.rows;
        let pageNumber: number = (e.first / e.rows) + 1;

        localStorage.setItem("similar-rows", e.rows.toString());

        this.dataProfileService.getMatchesByMatchType(this.assetUid, "Structure", pageNumber, pageSize, this.similarSimpleFilter)
            .subscribe((res) => {
                this.similarData = res.items;
                this.similarDataTotalCount = +res.total;
                this.similarDataLoading = false;
                this.cdRef.markForCheck();
            });
    }

    onMenuSelect(event, item) {
        if (event.value === "Open") {
            this.router.navigate([`${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${item.uid}`]);
        }

        if (event.value === "Open in New Tab") {
            window.open(`${SiteUrlHelpers.SITE_URL_ASSET_ROOT}/${item.uid}`, '_blank');
        }
    }

    formatPath(assetPath: string) {
        return assetPath.split('>').join(`&nbsp;&nbsp;<i class='fa fa-angle-right'></i>&nbsp;&nbsp;`);
    }

    get duplicateRows() {
        var savedVal = localStorage.getItem("duplicate-rows");
        return savedVal ? +savedVal : 10;
    }

    get similarRows() {
        var savedVal = localStorage.getItem("similar-rows");
        return savedVal ? +savedVal : 10;
    }
}