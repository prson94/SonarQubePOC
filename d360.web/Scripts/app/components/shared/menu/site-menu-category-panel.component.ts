import { Input, Component, ChangeDetectionStrategy, Output, EventEmitter, AfterViewInit, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../base.component';
import { SiteMenu, SiteMenuItem } from '../../../models/site-menu.model';
import * as _ from 'lodash';
import { SearchFieldComponent } from '../controls/search-field/search-field.component';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-site-menu-category-panel',
    templateUrl: 'site-menu-category-panel.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuCategoryPanelComponent extends BaseComponent implements AfterViewInit, OnChanges {
    @Input() menu: SiteMenu;
    @Input() isActive: boolean = false;

    @Output() contentSizeChanged = new EventEmitter();
    @Output() activeItemChanged = new EventEmitter();

    ngOnChanges(changes: SimpleChanges) {
        if (this.isActive) {
            if (this.searchInput) {
                this.searchInput.focus();
            }

            this.clearInput();
        }
    }

    public searchText: string = "";

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    get tooltipText(): string { return this.hideEmptyItems ? $localize`Show empty collections` : $localize`Hide empty collections`; }

    @ViewChild('searchinput', { static: false }) searchInput: SearchFieldComponent;

    toggleEmptyVisibility() {
        var newValue = (!this.hideEmptyItems).toString();
        localStorage.setItem(this.storageKey, newValue);
    }

    hideEmptySubItems(items: SiteMenuItem[]) {
        items.forEach((x) => {
            if (x.Items) {
                x.Items = x.Items.filter((y) => y.count > 0);
                this.hideEmptySubItems(x.Items);
            }
        });
    }

    _visibleMenuItems: SiteMenuItem[] = [];
    get visibleMenuItems(): SiteMenuItem[] {
        if (this.hideEmptyItems) {
            var menu = _.cloneDeep(this.menu.NavigationItems);
            var items = menu.filter((x) => x.count > 0);
            this.hideEmptySubItems(items);
            if (this.getTreeCount(this._visibleMenuItems) !== this.getTreeCount(items)) {
                this._visibleMenuItems = items;
            }
        }
        else {
            this._visibleMenuItems = this.menu.NavigationItems;
        }

        return this._visibleMenuItems;
    }

    getTreeCount(items: SiteMenuItem[]) {
        var cnt = items.length;
        items.forEach((node) => {
            if (node.Items) {
                cnt += this.getTreeCount(node.Items);
            }
        });
        return cnt;
    }

    get showVisiblityToggle(): boolean {
        return this.menu.ShowVisibilityToggle;
    }

    get hideEmptyItems(): boolean {
        return localStorage.getItem(this.storageKey) === "true";
    }

    get storageKey(): string {
        return "hide-empty-items-" + this.menu.MenuID;
    }

    getMaxHeight() {
        return (window.innerHeight - 80) + 'px';
    }

    ngAfterViewInit(): void {
        if (this.searchInput) {
            this.searchInput.focus();
        }
    }

    clearInput() {
        this.searchText = "";
    }
}