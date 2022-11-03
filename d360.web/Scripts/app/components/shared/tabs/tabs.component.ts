import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy, ViewChildren, QueryList, Input } from '@angular/core';
import { Router } from '@angular/router';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { Subscription } from 'rxjs';
import * as _ from 'lodash';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ArtifactService } from '../../../services/artifacts.service';
import { SearchDetail } from '../../../models/search-result.model';
import { Tab } from './tabs.models';

// TODO: it will be great to move out next out of this component: 
// • statistics
// • searchDetails
// • isScoringScreen 
// • area 
// • filterScoringTabHasNoValue
// • getTitle
@Component({
    selector: 'd3s-tabs',
    templateUrl: 'tabs.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ArtifactService],
    styleUrls: ['./tabs.component.less'],
    host: { '(window:resize)': 'checkSize()' }
})

export class TabsComponent implements OnDestroy {
    @Input() items: Tab[];
    @Input() showOnlyMainTab: boolean = false;
    @Input() hideMainTab = true;
    @Input() area = { icon: 'fa-folder', title: '' };
    @Input() emitSecondaryNav = false;
    @Input() statistics: ObjectStatistics;
    @Input() searchDetails: SearchDetail;
    @Input() isScoringScreen: boolean = false;

    homeUrlChangeSub: Subscription;
    homeUrl: string;

    routerUrlChangeSub: Subscription;
    private routerUrl: string = '';

    @ViewChildren('tabScroller') tabScroller: QueryList<ElementRef>;
    showScrollButtons: boolean = false;
    disableScrollLeft: boolean = false;
    disableScrollRight: boolean = false;

    constructor(
        private secondaryNavService: SecondaryNavService,
        private ref: ChangeDetectorRef,
        private router: Router) {
    }

    ngOnInit() {
        this.routerUrl = this.router.url;
        this.routerUrlChangeSub = this.router.events.subscribe(x => {
            this.routerUrl = this.router.url;
            this.ref.markForCheck();
        })

        this.homeUrlChangeSub = this.secondaryNavService.homeUrlChange$.subscribe(
            (item) => {
                this.homeUrl = item;
            }
        );
    }

    ngAfterViewInit() {
        this.checkSize();
    }

    isTabVisible = (tab: Tab) => {
        return (tab.isVisible != null ? tab.isVisible() : true)
            && this.filterScoringTabHasNoValue(tab);
    }

    filterScoringTabHasNoValue = (tab: Tab) => {
        if (tab.title === "Scoring") {
            return Boolean(this.searchDetails?.Scores.length);
        }
        return true;
    };

    checkSize() {
        if (this.tabScroller && this.tabScroller.length > 0) {
            let maxWidth = this.getElementRightPosition(this.tabScroller.first.nativeElement.parentElement);
            let lastTab = this.getElementRightPosition(this.tabScroller.first.nativeElement.lastElementChild);
            this.showScrollButtons = lastTab > maxWidth;
        }
        this.checkScrollPos();
    }

    checkScrollPos() {
        if (this.tabScroller && this.tabScroller.length > 0) {
            let currentPosition = this.tabScroller.first.nativeElement.scrollLeft;
            this.disableScrollLeft = currentPosition == 0;

            let maxWidth = this.getElementRightPosition(this.tabScroller.first.nativeElement.parentElement);
            let lastTab = this.getElementRightPosition(this.tabScroller.first.nativeElement.lastElementChild);
            this.disableScrollRight = lastTab <= maxWidth;

            this.ref.markForCheck();
        }
    }

    getElementRightPosition(element) {
        if (element && element.getBoundingClientRect) {
            return element.getBoundingClientRect().right;
        }
        return NaN;
    }

    scroll(direction: string) {
        let scrollAmount = 0;
        let scrollDistance = 300;
        let move = () => {
            if (direction == 'L') {
                this.tabScroller.first.nativeElement.scrollLeft -= 10;
            } else {
                this.tabScroller.first.nativeElement.scrollLeft += 10;
            }
            scrollAmount += 10;
            if (scrollAmount >= scrollDistance) {
                this.checkScrollPos();
                window.clearInterval(id);
            }
            this.checkScrollPos();
        };

        let id = window.setInterval(move, 5);
    }

    getTitle(item: Tab) {
        if (this.statistics && this.statistics.IssueCount > 0 && item.title === 'Actions') {
            let plurality = this.statistics.IssueCount == 1 ? ' is' : 's are';
            return this.statistics.IssueCount + " outstanding action" + plurality + " assigned to you";
        } else {
            return "";
        }
    }

    isTabActive(tab: Tab) {
        if (this.secondaryNavService.activeTabTitle === tab.title) {
            return true;
        }

        return this.routerUrl.startsWith(tab.url)
            || (tab.subTabsUrl ?? []).some(subTabUrl => this.routerUrl.startsWith(subTabUrl));
    }

    trackById(index, item) {
        return item.tag;
    }

    itemClicked(item: Tab) {
        if (this.isTabActive(item)) {
            return;
        }

        if (this.AllClosed()) {
            if (this.emitSecondaryNav) {
                this.secondaryNavService.setLocalHomeUrl(this.router.url);
            }

            this.homeUrl = this.router.url;
        }
        this.closeAll();
        if (item.title == "homeClick") {
            this.secondaryNavService.clearLocalActiveItem();

            let home = this.homeUrl ? this.homeUrl : this.secondaryNavService.getLocalHomeUrl();
            if (!home) {
                home = `/artifact/${this.secondaryNavService.artifactTypeId}`;
                this.secondaryNavService.activeTabTitle = null;
                this.secondaryNavService.artifactTypeId = null;
            }
            this.router.navigateByUrl(home);
            return;
        }

        if (item.url) { this.router.navigateByUrl(item.url); }

        if (this.emitSecondaryNav) {
            this.secondaryNavService.itemClicked(item as any);
        }

        this.AllClosed();
    }

    AllClosed() {
        let count = this.items.filter((x) => this.isTabActive(x)).length;
        if (count === 0) { this.secondaryNavService.setLocalActiveItem(undefined); }
        return count == 0;
    }

    closeAll() {
        for (let ritem of this.items) {
            if (this.isTabActive(ritem)) {
                if (this.emitSecondaryNav) {
                    this.secondaryNavService.itemClicked(ritem as any);
                }
            }
        }
    }

    ngOnDestroy() {
        this.routerUrlChangeSub?.unsubscribe();
        this.homeUrlChangeSub?.unsubscribe();
    }
}
