import {
	ChangeDetectionStrategy,
	Component,
	ElementRef,
	EventEmitter,
	HostListener,
	Input,
	Output,
	TemplateRef,
	ViewChild
} from '@angular/core';
import { BaseComponent } from '../base.component';
import { SiteMenu, SiteNav } from '../../../models/site-menu.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { Router } from '@angular/router';

@Component({
	selector: 'd3s-site-menu-category',
	templateUrl: 'site-menu-category.component.html',
	styleUrls: ['./site-menu-category.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class SiteMenuCategoryComponent extends BaseComponent {
	@Input() url: string;
	@Input() title: string;
	@Input() rootIconName: string;
	@Input() menu: SiteMenu;
	@Input() expanded: boolean;
	@Input() imageUrl: string;
	@Input() countData: any[];
	@Input() isActive: boolean = false;
	@Input() customPanelContent: TemplateRef<any>;
	@Input() emptyHint: TemplateRef<any>;

	@Output() clearClick = new EventEmitter();
	@Output() activeItemChanged = new EventEmitter();

	@HostListener('document:click', ['$event'])
	documentClick(event: MouseEvent) {
		if (this.menu && this.menu.isActiveItem) {
			this.activeItemChanged.emit(undefined);
		}
	}

	isCaretHovered = false;

	constructor(
		protected settingsService: CompanySettingsService,
		private router: Router) {
		super(settingsService);
	}

	@ViewChild('item', { static: false }) item: ElementRef<HTMLLIElement>;

	getDataCyAttribute() {
		return `PrimaryNav_${this.title}`;
	}

	navigateToUrl(url, $event: MouseEvent) {
		if (url) {
			this.router.navigateByUrl(url);
		}
		else if (!this.expanded) {
			this.onCategoryExpand($event);
		}
	}

	onCategoryExpand($event: MouseEvent) {
		$event.stopPropagation();

		if (this.menu && this.menu.isActiveItem) {
			this.activeItemChanged.emit(undefined);
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
}