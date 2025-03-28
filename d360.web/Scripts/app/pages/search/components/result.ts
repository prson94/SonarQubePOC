import {
	Component,
	ElementRef,
	EventEmitter,
	Input,
	OnInit,
	Output,
	ViewChild
} from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { BaseComponent } from '../../../components/shared/base.component';
import { PopupMenu, PopupMenuModule } from '../../../components/shared/controls/popup-menu/popup-menu.component';
import { SearchFullResult, SearchSelection, SearchResultFieldDisplay } from '../../../models/search-result.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SimpleBadgeModule } from '../../../components/shared/small-widgets/simple-badge/simple-badge.module';
import { ScoreBadgeModule } from '../../../components/shared/small-widgets/score-badge/score-badge.module';
import { AssetPath } from './asset-path';
import { TagViewModule } from '../../../components/shared/tags/d3s-tag-view.module';
import { CoreModule } from '../../../components/shared/core.module';
import { ScrollerWidget } from '../../../_shared/components/scroller-widget';

@Component({
	selector: 'result',
	templateUrl: './result.html',
	styleUrls: ["result.less"],
	standalone: true,
	imports: [
		AssetPath,
		CoreModule,
		PopupMenuModule,
		ScoreBadgeModule,
		ScrollerWidget,
		SimpleBadgeModule,
		TagViewModule
	]
})
export class ResultItem extends BaseComponent implements OnInit {
	@Input() result: SearchFullResult;
	@Input() selection: SearchSelection;

	@Output() onSelect = new EventEmitter();

	showStatus: boolean = false;
	showPath: boolean = false;

	menuitems: any[] = [{ title: $localize`Open` }, { title: $localize`Open in New Tab` }];

	@ViewChild('cardmenu', { static: false }) cardmenu: PopupMenu;

	constructor(private router: Router,
		private messagesService: MessagesObservableService,
		protected settingsService: CompanySettingsService,
		private elementRef: ElementRef,
		private datePipe: DatePipe) {
		super(settingsService);
	}

	ngOnInit() {
		this.loadDetails();
	}

	private loadDetails() {
		if (this.result.Status) {
			this.showStatus = true;
		}
	}

	parseTagResult(tags: any[]) {
		return tags.map((tag) => { return { uid: tag.Uid, Value: tag.Value }; });
	}

	get type() {
		if (this.result) {
			switch (this.result.Group) {
				case 'Reference':
					return 'ReferenceItemType';
				default:
					return this.result.Group;
			}
		}
	}

	showBadges(): boolean {
		return this.showStatus || this.result.Scores.length > 0;
	}

	clickMenuItem(event: any) {
		const key = event.value.toLowerCase();

		if (key === $localize`Open`.toLowerCase()) {
			this.navigateLink();
		}
		else if (key === $localize`Open in New Tab`.toLowerCase()) {
			this.navigateLink(true);
		}
	}

	navigateLink(newTab: boolean = false) {
		const url = SiteUrlHelpers.convertClassicUrl(this.result.Url);
		if (newTab) {
			// eslint-disable-next-line
			window.open(url, "_blank");
		} else {
			this.router.navigateByUrl(this.federateUrl(url));
		}
	}

	formatPathAsString(): string {
		if (this.result.Group && this.result.AssetPath) {
			return this.result.Group + ' > ' + this.result.AssetPath.map((p) => p.Key.join(' / ') + ' (' + p.AssetType + ')').join(' > ');
		}
		return '';
	}

	get isSelected(): boolean {
		return this.selection?.ID === this.result.ID;
	}

	get isSelectedCss(): string {
		return this.isSelected ? "selected" : "";
	}

	/**
	 * Formats display of field value.
	 * Links are returned from API in format <url>|<displayvalue>, Booleans are displayed as an icon etc.
	 * If Prefix/Suffic is set, they are added to the display value
	 * @param field
	 * @param forTitle Return is used in title, so booleans are shown as value and links shown as displayvalue
	 */
	getFieldDisplayValue(field: SearchResultFieldDisplay, forTitle: boolean = false): string {
		let val: string = (field.Empty) ? '---' : field.Value;
		if (val === null || val === undefined) { return ''; }

		if (!field.Empty) {
			switch (field.Type.toLowerCase()) {
				case 'link':
					if (field.Value.length > 2 && field.Value.indexOf('|') > 0) {
						const link: string[] = field.Value.split('|', 2);
						val = forTitle ? link[1] : '<a href="' + link[0] + '" target="_blank">' + link[1] + '</a>';
					}
					break;
				case 'boolean':
					if (!forTitle) {
						if (field.Value === 'True') { val = '<i class="fa fa-check enabled"></i>'; }
						else { val = '<i class="fa fa-times disabled"></i>'; }
					}
					break;
				case 'decimal':
				case 'number':
					val = Number(val).toLocaleString();
					break;
				case 'date':
					val = val.substr(0, val.indexOf(' '));
					break;
				case 'datetime':
					//Date is UTC
					const utc = Date.parse(val + ' UTC');
					val = this.datePipe.transform(utc, 'medium');
					break;
			}
		}
		if (field.Suffix) { val += ' ' + field.Suffix; }
		if (field.Prefix) { val = field.Prefix + ' ' + val; }
		return val;
	}

	/* events */
	onClick() {
		this.elementRef.nativeElement.children[0].focus();
	}

	onTouchEnd() {
		this.elementRef.nativeElement.children[0].focus();
	}

	onFocus() {
		this.onSelect.emit({
			ID: this.result.ID,
			AssetUid: this.result.Uid,
			ObjectType: this.result.Object,
			HasProfiling: this.result.HasProfiling,
			Data: this.result,
			IsNew: !this.isSelected
		});
	}

	onKeyDown(event: KeyboardEvent) {
		if (!this.cardmenu.isVisible && ["ArrowDown", "ArrowUp"].indexOf(event.key) !== -1) {
			event.preventDefault();
			const resultElement = this.elementRef.nativeElement.parentElement;
			const neighbor: HTMLDivElement = (event.key === "ArrowDown") ? resultElement.nextElementSibling : resultElement.previousElementSibling;
			neighbor?.querySelector<HTMLDivElement>(".card-res")?.focus();
		}
	}
}