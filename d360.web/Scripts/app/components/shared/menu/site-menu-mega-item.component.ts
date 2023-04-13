import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from "@angular/core";
import { Router } from '@angular/router';
import { BaseComponent } from '../base.component';
import { SiteMenuService } from '../../../services/site-menu.service';
import { NavigationState, SiteMenuItem } from '../../../models/site-menu.model';
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-site-menu-mega-item',
    templateUrl: './site-menu-mega-item.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuMegaItemComponent extends BaseComponent {

    @Input() item: SiteMenuItem;
    @Input() category: string;
    @Input() parentUrl: string;
    @Input() level: number;
    @Input() active: boolean;
    @Input() count: number;
    @Input() searchText: string;
    @Output() activeChange = new EventEmitter();
	numberLoading: boolean;
	noReadTooltip: string = $localize`You do not have read access to this type`;

    constructor(
        private menuService: SiteMenuService,
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
    }

    getSubIndent() {
        if (this.level > 0 && this.item.Items == null)
            {return ((this.level + 1) * 20) + 'px';}
        if (this.level > 0 && this.item.Items != null)
            {return '0px';}
        else
            {return null;}
    }

    getMainIndent() {
        if (this.item.Items && this.level === 0)
            {return '0px';}
        else if (this.level > 0 && this.item.Items == null)
            {return ((this.level + 1) * 20) + 'px';}
        else if (this.level > 0 && this.item.Items != null)
            {return ((this.level) * 20) + 'px';}
        else
            {return '20px';}

    }

    handleArrowClick(event) {
        event.stopPropagation();

        if (!this.item.ShowChildren) {
            this.item.ShowChildren = true;
            this.showChildElements();
        }
        else {
            this.item.ShowChildren = false;
            this.hideChildElements();
        }
    }

	itemClick() {
		if (this.item.Url == null || this.item.Disabled)
            {return;}

        if (this.item.IsLink) {
            window.location.href = this.item.Url;
        } else {
			this.router.navigateByUrl(this.federateUrl(this.item.Url));
        }
        this.active = false;
        this.activeChange.emit(this.active);
    }

    showChildElements() {
        const nav: NavigationState[] = JSON.parse(localStorage.getItem("NavigationMenu"));

        //check if there's already a branch for this category
        if (nav.some((x) => x.SiteMenuID === this.category)) {
            nav.forEach((menu) => {
                if (menu.SiteMenuID === this.category) {
                    menu.DisplayElements.push({ ParentUrl: this.parentUrl, Url: this.item.Url ? this.item.Url : this.item.Name });
                }
            });
        } else {
            //add new category
            nav.push({ SiteMenuID: this.category, DisplayElements: [{ ParentUrl: this.parentUrl, Url: this.item.Url ? this.item.Url : this.item.Name }] });
        }

        localStorage.setItem("NavigationMenu", JSON.stringify(nav));
    }

    hideChildElements() {
        const nav: NavigationState[] = JSON.parse(localStorage.getItem("NavigationMenu"));

        nav.forEach((menu) => {
            if (menu.SiteMenuID === this.category) {
                menu.DisplayElements.splice(menu.DisplayElements.findIndex((element) => (element.ParentUrl === this.parentUrl && element.Url === this.item.Url) || (!element.ParentUrl && element.Url === this.item.Name)), 1);
            }
        });

        localStorage.setItem("NavigationMenu", JSON.stringify(nav));
    }
}