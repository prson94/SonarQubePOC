import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    Output,
    SimpleChange
} from '@angular/core';
import { SiteUrlHelpers } from '../../../../static/site-url-helpers';
import { Router } from '@angular/router';
import { LinkClickInterceptor } from '../../../../services/href-click-service';
import { CompanySettingsService } from "../../../../services/settings.service";
import { StringConstants } from '../../../../static/string-constants';
import { AuthenticationService } from '../../../../services/authentication.service';
import { Tag, TagType } from '../../../../models/tag.model';
import { FeatureFlagsInitService } from '../../../../services/feature-flags-init.service';
import { FeatureFlags } from '../../../../_shared/models/feature-flags';

@Component({
    selector: 'ig-tag-detail',
	templateUrl: './tag-detail.component.html',
	styleUrls: ['./tag-detail.component.less'],
    host: {
        "(document:click)": "clickedOutside($event)",
    },
})

export class TagDetailComponent implements OnChanges {
    @Input() useAccordion: boolean = false;
    @Input() shouldBePadded: boolean = true;
    @Input() tooltipAlign: string;
    @Input() showHeader: boolean = false;
    @Input() showTabs: boolean = true;
    @Input() showHeaderLine: boolean = true;
    @Input() spacerHeight: string = '32px';
    @Input() isSidePanel: boolean = false;
    @Input() hasEditLink: boolean = false;
    @Input() hasOpenLink: boolean = true;
    @Input() interceptLinkClick: boolean = false;
    @Input() hideLinks: boolean = false;
    @Input() hideClassName: boolean = false;
    @Input() showOnlyFields: Set<string> = null;

    //baseAssetUid is used to determine on which side is our relationship
	@Input() baseTagUid: string = '';
	@Input() selectedItem: any = TagType;
	@Input() selectedTagType: any = Tag;
    @Output() onEditClick = new EventEmitter();
	@Output() close = new EventEmitter();
	@Output() onLinkClicked = new EventEmitter();

    tagUID: string;
    tagTypeUID: string;
    tagUrl: string;
    isLoading = false;

    readonly systemProperties: string = $localize`System Fields`;
    readonly noCategory: string = $localize`None`;
    readonly defaultCategory: string = $localize`General`;

    subtitle: string = "";

    model: any;
    tab: string = 'detail';
    simpleSearchTooltipHTML: string = StringConstants.simpleSearchTooltipHTML;

    isAdmin: boolean = false;

	newSecurityEnabledFeatureFlag: boolean = true;

    constructor(
        private router: Router,
		protected settingsService: CompanySettingsService,
        private linkClickInterceptor: LinkClickInterceptor,
		private authService: AuthenticationService,
		private featureFlagService: FeatureFlagsInitService,
		private cdRef: ChangeDetectorRef) {
		this.authService.checkCurrentUserAdmin().subscribe((res) => { this.isAdmin = res; });

		featureFlagService.getFlagValue(FeatureFlags.NewSecurityModel).then((flag) => {
			this.newSecurityEnabledFeatureFlag = flag;
		});
	}

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		this.tab = 'detail';
    }

	private loadUrl() {
		this.tagUrl = `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${this.baseTagUid}`;
       }

    get storageKey(): string {
		return `tag_detail_${this.settingsService.CurrentResourceID}_${this.tagTypeUID}`;
    }

	open(newTab: boolean = false) {
		const openUrl = this.tagUrl;
		if (openUrl) {
            if (newTab) {
				window.open(openUrl, '_blank');
			} else {
				this.router.navigateByUrl(SiteUrlHelpers.federateUrl(openUrl));
            }
        }
    }

    clickTab(key: string) {
        this.tab = key;
    }

    clickedOutside(event: any) {
		if (!(event.composedPath().filter((f) => f?.classList?.contains("secondary-side-panel")).length > 0)) {
            this.close.emit();
        }
	}

	resourceClicked(uid: string) {
		this.onLinkClicked.emit({ uid, type: 'Resource' });
	}
}
