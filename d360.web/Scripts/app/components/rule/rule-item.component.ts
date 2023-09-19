import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { RulesService } from '../../services/rules.service';
import { PermissionsService } from '../../services/permissions.service';
import { RuleDetail } from '../../models/rule.model';
import { StringConstants } from '../../static/string-constants';
import { finalize } from 'rxjs/operators';
import { SiteUrlHelpers } from "../../static/site-url-helpers";
import { Subscription } from 'rxjs';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { CompanySettingsService } from '../../services/settings.service';
import { LinkClickInterceptor } from '../../services/href-click-service';
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { UsageAction } from '../../models/web-analytics-activity.model';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { MessagesObservableService } from '../../services/messages-observable.service';

@Component({
	selector: 'd3s-rule-item',
	providers: [RulesService, PermissionsService, WebAnalyticsService],
	templateUrl: 'rule-item.component.html'
})

export class RuleItemComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() assetUid: string;

	private routeParamsSubscription: any;
	private rule: RuleDetail;

	hrefSub: Subscription;
	selectedAsset: any;
	selectedReferenceItem: any;
	selectedTag: any;

    sidePanelOpen: boolean = false;
    sidePanelStorageKey = 'side_panel_width_detail_rules';

	constructor(private rulesService: RulesService,
		private route: ActivatedRoute,
		private sidePanelService: SidePanelService,
		private router: Router,
		secondaryNavService: SecondaryNavService,
		protected titleService: Title,
		protected headerBreadcrumbService: HeaderBreadcrumbService,
		protected permissionsService: PermissionsService,
		protected settingsService: CompanySettingsService,
		webAnalyticsService: WebAnalyticsService,
		private linkClickInterceptor: LinkClickInterceptor,
		launchDarklyService: LaunchDarklyService,
		private messagesService: MessagesObservableService
	) {
		super(settingsService);

		this.webAnalyticsService = webAnalyticsService;
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = headerBreadcrumbService;
		this.launchDarklyService = launchDarklyService;
	}

	ngOnInit() {

		this.isLoading = true;
		this.logAssetAction(UsageAction.View, this.assetUid);

		this.permissionsService.getAssetPermissions(this.assetUid)
			.subscribe((res) => {
				this.objectPermission = res;
				this.load();
			}
			);

		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this, ev);
		});

	}

    getSidePanelWidth(): number {
        return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
    }

    getSidePanelMaxWidth(): number {
        return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
    }

    getSidePanelMinWidth(): number {
        return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
    }

    onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
        this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
    }

    ngOnDestroy() {
        if (this.routeParamsSubscription) {
            this.routeParamsSubscription.unsubscribe();
        }
    }

	load() {
		this.rulesService.getRuleApplyPremission(this.assetUid).pipe(
			finalize(() => {
				this.isLoading = false;
			})
			).subscribe((result) => {
				if (result) {
					this.rule = result;

					this.setBrowserTitle(this.titleService, this.rule.Name);
					console.log(this.rule);
					this.headerBreadcrumbService.setCurrentObjectInfo('Rule', this.rule.ID, null, this.rule.UID);
					this.setObjectInfo('Rule', this.rule.ID, this.rule.Name, this.rule.AssetID, undefined, this.rule.UID);

					this.loadPermissions(this.permissionsService, StringConstants.ObjectRule, this.rule.ID).then((p) => {
						this.buildSecondaryNavigation({ assetUid: this.rule.UID, DisplayValue: this.rule.Name });
					});
				}
				else {
					this.isLoading = false;
					this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
					this.messagesService.showError($localize`Error`, $localize`Rule with Uid could not be found or you do not have permission to view the related rule.`);
				}
			},
				(err) => {
				this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
			});
	}

	editRule() {
		this.load();
	}
}
