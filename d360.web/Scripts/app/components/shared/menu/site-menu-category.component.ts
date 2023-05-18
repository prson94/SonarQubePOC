import {
	ChangeDetectionStrategy,
	Component,
	ElementRef,
	EventEmitter,
	HostListener,
	Input,
	OnChanges,
	Output,
	TemplateRef,
	ViewChild
} from '@angular/core';
import { BaseComponent } from '../base.component';
import { SiteMenu, SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { Router } from '@angular/router';
import * as DOMPurify from "dompurify";

@Component({
	selector: 'd3s-site-menu-category',
	templateUrl: 'site-menu-category.component.html',
	styleUrls: ['./site-menu-category.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuCategoryComponent extends BaseComponent implements OnChanges {
	@Input() url: string;
	@Input() title: string;
	@Input() rootIconName: string;
	@Input() menu: SiteMenu;
	@Input() expanded: boolean;
	@Input() imageUrl: string;
	@Input() countData: unknown[];
	@Input() isActive: boolean = false;
	@Input() customPanelContent: TemplateRef<unknown>;
	@Input() emptyHint: TemplateRef<unknown>;

	@Output() clearClick = new EventEmitter();
	@Output() activeItemChanged = new EventEmitter();
	menuTooltip: string;
	public showCaret: boolean = true;

	@HostListener('document:click', ['$event'])
	documentClick() {
		if (this.menu && this.menu.isActiveItem) {
			this.activeItemChanged.emit(null);
		}
	}

	constructor(
		protected settingsService: CompanySettingsService,
		private router: Router) {
		super(settingsService);
	}

	ngOnChanges(): void {
		this.menuTooltip = "";
		if(this.expanded){
			this.showCaret = true;
		}
		
		if (this.menu && this.menu.Description) {
			this.menuTooltip = DOMPurify.sanitize(this.menu.Description);
			if (!this.expanded) {
				this.menuTooltip = DOMPurify.sanitize(`<p><b>${this.title}</b></p>${this.menu.Description}`);
				this.showCaret = false;
			}
		} else {			
			if (!this.expanded) {
				this.menuTooltip = this.title;
				this.showCaret = false;
			}			
		}
    }

	@ViewChild('item', { static: false }) item: ElementRef<HTMLLIElement>;

	getDataCyAttribute() {
		return `PrimaryNav_${this.title}`;
	}

	navigateToUrl(url, $event: MouseEvent) {
		if (url) {
			this.router.navigateByUrl(this.federateUrl(url));
		}
		else {
			this.onCategoryExpand($event);
		}
	}

	onCategoryExpand($event: MouseEvent) {
		$event.stopPropagation();

		if (this.menu && this.menu.isActiveItem) {
			this.activeItemChanged.emit(null);
		} else {
			this.activeItemChanged.emit({ item: this });
			this.positionMenu();
		}
	}

	positionMenu() {
		if (!this.menu || !this.menu.NavigationItems) {
			return;
		}

		const submenu = this.item.nativeElement.children[0].nextElementSibling as HTMLDivElement;
		if (!submenu) {
			return;
		}

		this.menu.isActiveItem = true;
		submenu.style.zIndex = (SiteNav.zindex + 1).toString();
		submenu.style.left = this.item.nativeElement.offsetWidth + 'px';

		this.repositionMenuToFit();
		window.setTimeout(() => {
			this.repositionMenuToFit();
		}, 150);
	}

	stopNavigation(event) {
		event.stopPropagation();
	}

	repositionMenuToFit() {
		const wantedPanelTop = this.item.nativeElement.getBoundingClientRect().top;

		const panel = this.item.nativeElement.children[0].nextElementSibling as HTMLDivElement;
		const panelRect = panel?.getBoundingClientRect();

		const panelBottomEstimate = wantedPanelTop + panelRect?.height;
		const overflow = Math.max(0, panelBottomEstimate - window.innerHeight);
		const newPanelTop = Math.max(0, wantedPanelTop - overflow);

		if (panel) {
			panel.style.top = newPanelTop + 'px';
		}
	}

	toggleShowCaret(isHovered: boolean) {
		if (!this.expanded) {
			this.showCaret = isHovered;
		}		
	}
}