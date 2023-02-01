import { Component, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, OnDestroy, ViewChildren, QueryList, Input } from '@angular/core';
import { Router } from '@angular/router';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { Subscription } from 'rxjs';
import { ObjectStatistics } from '../../../models/object-statistics.model';
import { ArtifactService } from '../../../services/artifacts.service';
import { SearchDetail } from '../../../models/search-result.model';
import { Tab } from './tabs.models';
import { SecondaryNavItem } from '../../../models/secondaryNav.model';
import { CompanySettingsService } from "../../../services/settings.service";
import { AuthenticationService } from '../../../services/authentication.service';
import { CompanySettingEnum } from '../../../models/settings.model';

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
    @Input() showOnlyMainTab = false;
    @Input() hideMainTab = true;
    @Input() area = { icon: 'fa-folder', title: '', tabTitle: '' };
    @Input() emitSecondaryNav = false;
    @Input() statistics: ObjectStatistics;
    @Input() searchDetails: SearchDetail;
    @Input() isScoringScreen = false;

    homeUrlChangeSub: Subscription;
    homeUrl: string;

    routerUrlChangeSub: Subscription;
    private routerUrl = '';

    @ViewChildren('tabScroller') tabScroller: QueryList<ElementRef>;
    showScrollButtons = false;
    disableScrollLeft = false;
    disableScrollRight = false;

    constructor(
        private secondaryNavService: SecondaryNavService,
        private ref: ChangeDetectorRef,
		private router: Router,
		private settingsService: CompanySettingsService,
		private authenticationService: AuthenticationService) {
    }

    get visibleItems() {
        if (!this.items) {
            return [];
        }

        return this.items.filter((item) => {
            if (item.isVisible) {
                return item.isVisible();
            }

            return true;
        });
    }

    ngOnInit() {
        this.routerUrl = this.router.url;
        this.routerUrlChangeSub = this.router.events.subscribe(() => {
            this.routerUrl = this.router.url;
            this.ref.markForCheck();
        });

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
        let visible = (tab.isVisible != null ? tab.isVisible() : true)
			&& this.filterScoringTabHasNoValue(tab);

		if (tab.title === $localize`Comments`) {
			visible = visible && (this.authenticationService.isAdmin || this.settingsService.getSettingById(CompanySettingEnum.ShowResources).BooleanSetting.Value);
		}

		return visible;
    }

    filterScoringTabHasNoValue = (tab: Tab) => {
		if (tab.title === $localize`Scoring`) {
            return Boolean(this.searchDetails?.Scores.length);
        }
        return true;
    };

    checkSize() {
        if (this.tabScroller && this.tabScroller.length > 0) {
            const maxWidth = this.getElementRightPosition(this.tabScroller.first.nativeElement.parentElement);
            const lastTab = this.getElementRightPosition(this.tabScroller.first.nativeElement.lastElementChild);
            this.showScrollButtons = lastTab > maxWidth;
        }
        this.checkScrollPos();
    }

    checkScrollPos() {
        if (this.tabScroller && this.tabScroller.length > 0) {
            const currentPosition = this.tabScroller.first.nativeElement.scrollLeft;
            this.disableScrollLeft = currentPosition === 0;

            const maxWidth = this.getElementRightPosition(this.tabScroller.first.nativeElement.parentElement);
            const lastTab = this.getElementRightPosition(this.tabScroller.first.nativeElement.lastElementChild);
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
        const scrollDistance = 300;

        const move = () => {
            if (direction === 'L') {
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

        const id = window.setInterval(move, 5);
    }

    getTitle(item: Tab) {
        if (this.statistics && this.statistics.IssueCount > 0 && item.title === 'Actions') {
            const plurality = this.statistics.IssueCount === 1 ? ' is' : 's are';
            return this.statistics.IssueCount + " outstanding action" + plurality + " assigned to you";
        } else {
            return "";
        }
    }

    isTabActive(tab: Tab) {
        if (this.secondaryNavService.activeTabTitle === tab.title) {
            return true;
        }

        return this.routerUrl === tab.url
            || (tab.subTabsUrl ?? []).some((subTabUrl) => this.routerUrl.startsWith(subTabUrl));
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
        if (item.title === "homeClick") {
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
            this.secondaryNavService.itemClicked(item as SecondaryNavItem);
        }

        this.AllClosed();
    }

    AllClosed() {
        const count = this.items.filter((x) => this.isTabActive(x)).length;
        if (count === 0) { this.secondaryNavService.setLocalActiveItem(undefined); }
        return count === 0;
    }

    closeAll() {
        for (const ritem of this.items) {
            if (this.isTabActive(ritem)) {
                if (this.emitSecondaryNav) {
                    this.secondaryNavService.itemClicked(ritem as SecondaryNavItem);
                }
            }
        }
    }

    ngOnDestroy() {
        this.routerUrlChangeSub?.unsubscribe();
        this.homeUrlChangeSub?.unsubscribe();
	}

	get isMainTabVisible() {

		return !this.isScoringScreen && !this.hideMainTab && this.area?.tabTitle !== $localize`Relationship Types`;
	}
}
