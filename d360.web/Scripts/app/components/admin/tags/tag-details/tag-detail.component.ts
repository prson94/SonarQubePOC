import {
    ChangeDetectorRef,
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnDestroy,
    Output,
    QueryList,
    SimpleChange,
    ViewChildren
} from '@angular/core';

import { SiteUrlHelpers } from '../../../../static/site-url-helpers';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { GroupService } from '../../../../services/group.service';
import { LinkClickInterceptor } from '../../../../services/href-click-service';
import { CompanySettingsService } from "../../../../services/settings.service";
import { StringConstants } from '../../../../static/string-constants';
import { AuthenticationService } from '../../../../services/authentication.service';
import { mergeMap } from "rxjs/operators";
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from '../../../../services/feature-flags.enum';
import { Tag, TagType } from '../../../../models/tag.model';
import { PropertyGroupComponent } from '../../../shared/controls/property-group/property-group.component';

@Component({
    selector: 'ig-tag-detail',
	templateUrl: './tag-detail.component.html',
	styleUrls: ['./tag-detail.component.less'],
    host: {
        "(document:click)": "clickedOutside($event)",
    },
})

export class TagDetailComponent implements OnChanges, OnDestroy {
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
		private featureFlagService: LaunchDarklyService,
		private cdRef: ChangeDetectorRef) {
		this.authService.checkCurrentUserAdmin().subscribe((res) => { this.isAdmin = res; });
		this.newSecurityEnabledFeatureFlag = this.featureFlagService.variation<boolean>(FeatureFlags.NewSecurityModel);
	}

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		this.tab = 'detail';
    }

    ngOnDestroy() {
    }

	private loadUrl() {
		this.tagUrl = `${SiteUrlHelpers.SITE_URL_TAG_ROOT}/${this.baseTagUid}`;
       }

    get storageKey(): string {
		return `tag_detail_${this.settingsService.CurrentResourceID}_${this.tagTypeUID}`;
    }

	open(newTab: boolean = false) {
		let openUrl = this.tagUrl;
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
