import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit, OnDestroy } from "@angular/core";
import { Subscription } from "rxjs";
import { AssetService } from '../../../../services/asset.service';
import { AuthenticationService } from '../../../../services/authentication.service';
import { FeatureFlags } from "../../../../services/feature-flags.enum";
import { Permissions, PermissionsService } from '../../../../services/permissions.service';
import { Tab } from "../../../shared/tabs/tabs.models";

/*global $localize*/

@Component({
	selector: "d3s-reference-item-type-tabs",
	templateUrl: './reference-tabs.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReferenceItemTypeTabsComponent implements OnInit, OnDestroy {
	@Input() uid: string;

	itemCount: number = null;
	isAdmin: boolean = false;
	typePermission: Permissions;
	private countSubscripton: Subscription;
	private permissionSubscription: Subscription;

	constructor(
		private cdRef: ChangeDetectorRef,
		private assetService: AssetService,
		private authenticationService: AuthenticationService,
		private permissionsService: PermissionsService
	) { }

	get tabs(): Tab[] {
		const baseUrl = `/reference/${this.uid}`;
		return [
			{
				url: `${baseUrl}/details`,
				title: $localize`Details`,
				tag: "details"
			},
			{
				url: `${baseUrl}/items`,
				title: $localize`Items`,
				tag: "items",
				count: this.itemCount
			},
			{
				url: `${baseUrl}/fields`,
				title: $localize`Fields`,
				tag: "fields",
				isVisible: () => this.isAdmin,
			},
			{
				url: `${baseUrl}/owners`,
				title: $localize`Responsibilies`,
				tag: "owners",
				isVisible: () => this.isAdmin || this.typePermission.ReadResponsibilities,
			},
			{
				url: `${baseUrl}/relationships`,
				title: $localize`Relationships`,
				tag: "relationship",
				isVisible: () => this.isAdmin || this.typePermission.ReadRelationships,
			},
			FeatureFlags.AssignmentsFlag?this.assignmentTab(baseUrl):this.workflowTab(baseUrl),
			{
				url: `${baseUrl}/log`,
				title: $localize`Change Log`,
				tag: "Change Log",
				isVisible: () => true,
			}
		];
	}

	workflowTab(baseUrl:string){
		return({
			url: `${baseUrl}/workflow`,
			title: $localize`Workflow`,
			tag: "monitor",
			isVisible: () => true,
		})
	}

	assignmentTab(baseUrl:string){
		return ({
			url: `${baseUrl}/assignments`,
			title: $localize`Assignments`,
			tag: "Assignments",
			isVisible: () => true,
		})
	}

	ngOnInit() {
		this.isAdmin = this.authenticationService.isAdmin;
		this.countSubscripton = this.assetService.getAssetCountsByAssetTypeUid(this.uid).subscribe((res) => {
			if (res[0]) {
				this.itemCount = res[0].count;
				this.cdRef.markForCheck();
			}
		});

		this.permissionSubscription = this.permissionsService.getAssetTypePermissions(this.uid)
			.subscribe((res) => {
				this.typePermission = res;
				this.cdRef.markForCheck();
			});

	}

	ngOnDestroy() {
		this.countSubscripton?.unsubscribe();
		this.permissionSubscription?.unsubscribe();
	}
}
