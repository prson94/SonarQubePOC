import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, ChangeDetectionStrategy, OnDestroy, ViewEncapsulation } from '@angular/core';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetService } from '../../../services/asset.service';
import { forkJoin, Subscription } from 'rxjs';
import { TagService } from '../../../services/tag.service';
import { TagDetail, TagType } from '../../../models/tag.model';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { Router } from '@angular/router';
import { StringConstants } from '../../../static/string-constants';
import { AuthenticationService } from '../../../services/authentication.service';

@Component({
    selector: 'ig-tagged-assets-detail',
    templateUrl: './tagged-assets-detail.component.html',
    providers: [ObjectDetailService, AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush,
    styles: [`.p-datatable-wrapper { overflow:auto; } .p-datatable-wrapper table { table-layout: unset !important;  }
        .tagged-assets { padding:16px 0 16px 16px ; } .row-header { padding-bottom: 8px;}`],
    encapsulation: ViewEncapsulation.None
})


export class TaggedAssetDetailComponent implements OnChanges, OnDestroy {
    @Input() uid: string;
    isLoading: boolean = false;

    isAdmin: boolean = false;
    loadSub: Subscription;
    tab: string = 'items';
    tag: TagType;
    tagUsage: TagDetail[];
    filters: any = { globalSearch: '', DisplayValue: '', AssetType: '', TagsAsString: '' };

    simpleSearchTooltipHTML: string = StringConstants.simpleSearchTooltipHTML;

    constructor(
        protected messagesService: MessagesObservableService,
        private tagService: TagService,
        private router: Router,
        private linkClickInterceptor: LinkClickInterceptor,
        private authService: AuthenticationService,
        private cdRef: ChangeDetectorRef) {
        this.authService.checkCurrentUserAdmin().subscribe((res) => {
            this.isAdmin = res;
        });
    }

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
        this.loadSub = forkJoin(
            this.tagService.getTagByUid(this.uid),
            this.tagService.getTagDetails(this.uid)
        ).subscribe((res) => {
            this.tag = res[0];
            this.tagUsage = res[1].items;
            this.isLoading = false;
            this.cdRef.detectChanges();
        });
    }

    formatValue(item: TagDetail) {
        return item.AssetType.replace(':', ` <i class='fa fa-angle-right'></i> `);
    }

    navigate(item: any, $event) {
        this.linkClickInterceptor.sendEvent($event, item, "asset/" + item.AssetUid);
    }

    onFilterChange(event) {
        this.filters[event.prop] = event.value;
    }

    export() {
        this.tagService.exportTagsByUid(this.tag.uid, '', this.filters, '');
    }

    open(isNewTab: boolean = false) {
        let url : string = 'tag/' + this.tag.uid;
        if (!isNewTab) {
            this.router.navigateByUrl(url);
            return;
        } else {
            window.open(url, '_blank');
            return;
        }
    }
}
