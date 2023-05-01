import {
	ChangeDetectionStrategy,
	ChangeDetectorRef,
	Component,
	EventEmitter,
	Input,
	OnChanges,
	OnDestroy,
	Output,
	SimpleChange,
	ViewEncapsulation
} from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin, Subscription } from 'rxjs';
import { AuthenticationService } from '../../../services/authentication.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AssetTypeService } from '../../../services/asset-type.service';
import {
	AssetTypeDetailCategory,
	AssetTypeDetailField,
	AssetTypeDetailFieldType,
	ControlsOptions,
	OpenBehaviour
} from './asset-type-detail-v2.model';
import { AssetTypeApiModel, AssetTypeClass } from "../../../models/asset.model";
import { RelationshipsService } from '../../../services/relationships.service';
import { Predicate } from '../../../models/predicate.model';
import { AssetService } from '../../../services/asset.service';
import { SelectItem } from 'primeng/api';

/*global $localize*/

declare const CurrentResourceID;

@Component({
	selector: 'ig-asset-type-detail-v2',
	templateUrl: './asset-type-detail-v2.component.html',
	providers: [AssetTypeService, AssetService],
	changeDetection: ChangeDetectionStrategy.OnPush,
	styleUrls: ['asset-type-detail-v2.component.less'],
	encapsulation: ViewEncapsulation.None
})


export class AssetTypeDetailV2Component implements OnChanges, OnDestroy {
	@Input() uid: string;
	@Input() controlsOptions: ControlsOptions = { showEdit: true, showOpen: OpenBehaviour.NEW_TAB };
	@Output() onEditClick: EventEmitter<string> = new EventEmitter<string>();

	@Input() isDetailsPage: boolean = false;

	assetTypeModel: AssetTypeApiModel;
	categories: AssetTypeDetailCategory[] = [];
	subscription: Subscription;

	synonyms: Predicate[] = [];
	defaultColors: SelectItem[] = [];

	isAdmin: boolean;
	isLoading: boolean;

	formParentName: string = '';
	isModalVisible: boolean = false;

	onEditFormClose() {
		this.isModalVisible = false;

	}
	onEditSaveFinished() {
		this.load();
	}
	constructor(
		private router: Router,
		private assetTypeService: AssetTypeService,
		private assetsService: AssetService,
		private authService: AuthenticationService,
		private relationshipService: RelationshipsService,
		private cdRef: ChangeDetectorRef
	) {
		this.authService.checkCurrentUserAdmin().subscribe((res) => {
			this.isAdmin = res;
		});
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes.uid) {
			this.load();
		}
	}

	ngOnDestroy() {
		this.subscription?.unsubscribe();
	}

	public load(): void {
		this.isLoading = true;
		this.subscription =
			forkJoin(
				this.assetTypeService.GetAssetTypeByUid(this.uid),
				this.assetsService.getAllColors(),
				this.relationshipService.getPredicates('Grammar')).subscribe((data) => {
					this.assetTypeModel = data[0];
					this.defaultColors = data[1];
					this.synonyms = data[2];
					this.categories = [];
					if (this.assetTypeModel) {
						this.fillCategories(this.assetTypeModel);
						this.loadState();
						this.isLoading = false;
						this.cdRef.markForCheck();
					}
				});
	}

	onStateSave() {
		localStorage.setItem(
			this.localStorageKey,
			JSON.stringify(
				this.categories.map((category) => {
					return { name: category.name, active: category.active };
				})
			)
		);
	}

	private loadState() {
		const states: { name: string; active: boolean }[] = JSON.parse(localStorage.getItem(this.localStorageKey));
		if (states !== null) {
			states.forEach((state) => {
				const category = this.categories.find((category) => category.name === state.name);
				if (category) {
					category.active = state.active;
				}
			});
		}
	}

	get localStorageKey(): string {
		return `asset_type_detail_${CurrentResourceID}_${this.uid}`;
	}

	onAssetTypeOpen(isNewTab = false) {
		const openUrl = this.isAdmin ? SiteUrlHelpers.getAssetTypeConfigurationUrl(
			AssetTypeClass[this.assetTypeModel.Class.ID],
			this.uid
		) : SiteUrlHelpers.getAssetTypeUrl(this.uid);
		if (isNewTab) {
			window.open(openUrl, '_blank');
		} else {
			this.router.navigateByUrl(SiteUrlHelpers.federateUrl(openUrl));
		}
	}

	private sortCategories(categories: AssetTypeDetailCategory[]): AssetTypeDetailCategory[] {
		const priority = [
			$localize`General`,
			$localize`Security`,
			$localize`Styles`,
			$localize`Grammatic Type Allocation`,
			$localize`System Fields`
		];
		return categories.sort((a, b) => {
			return priority.indexOf(a.name) - priority.indexOf(b.name);
		});
	}

	private addFieldsToCategory(name: string, fields: AssetTypeDetailField[]): void {
		const category = this.categories.find((category) => category.name === name);
		if (category) {
			category.fields.push(...fields);
		} else if (fields.length > 0) {
			this.categories.push({
				name,
				active: true,
				fields
			});
			this.categories = this.sortCategories(this.categories);
		}
	}

	private fillBasicCategories(assetTypeModel: AssetTypeApiModel): void {
		this.addFieldsToCategory($localize`General`, [
			{
				name: $localize`Name`,
				type: AssetTypeDetailFieldType.TEXT,
				value: assetTypeModel.Name
			},
			{
				name: $localize`Display Format`,
				type: AssetTypeDetailFieldType.TEXT,
				value: assetTypeModel.DisplayFormat,
				tooltip: $localize`The value of this field token is used to reference the asset throughout the application, for example when you open an asset details page, the value of this field token is displayed in the breadcrumb`
			}
		]);

		this.fillParentRelationshipCategories(assetTypeModel);

		if (assetTypeModel.Class.ID !== AssetTypeClass.DiagramAsset) {
			this.addFieldsToCategory($localize`General`, [
				{
					name: $localize`Description`,
					type: AssetTypeDetailFieldType.HTML,
					value: assetTypeModel.Description
				},
				{
					name: $localize`Show Description on List Page`,
					type: AssetTypeDetailFieldType.BOOL,
					value: assetTypeModel.IsDescriptionEnabled
				}
			]);


			if (assetTypeModel.IsDescriptionEnabled) {
				this.addFieldsToCategory($localize`General`,
					[
						{
							name: $localize`Description Button Name`,
							type: AssetTypeDetailFieldType.TEXT,
							value: assetTypeModel.DescriptionButtonName ?? $localize`Information`
						},
						{
							name: $localize`Collapsed by default`,
							type: AssetTypeDetailFieldType.BOOL,
							value: assetTypeModel.IsDescriptionVisibleByDefault
						}]
				);
			}
		}

		if (assetTypeModel.Class.ID !== AssetTypeClass.DiagramAsset && assetTypeModel.Class.ID !== AssetTypeClass.Reference) {
			this.addFieldsToCategory($localize`Security`, [
				{
					name: $localize`Default Read Access`,
					type: AssetTypeDetailFieldType.BOOL,
					value: assetTypeModel.IsDefaultReadAccessEnabled,
					tooltip: $localize`If enabled, users without any responsibilities will see this asset type by default.`
				}
			]);
		}

		const defColor = this.defaultColors.find((c) => c.title.toLowerCase() === assetTypeModel?.IconStyle?.BackColor.toLowerCase());
		const backColorValue = {
			title: (defColor ? defColor.value : $localize`Custom`),
			value: assetTypeModel?.IconStyle?.BackColor
		};

		if (assetTypeModel.Class.ID !== AssetTypeClass.DiagramAsset) {
			this.addFieldsToCategory($localize`Styles`, [
				{
					name: $localize`Background Color`,
					type: AssetTypeDetailFieldType.COLOR,
					value: backColorValue,
					tooltip: $localize`Sets the background color of icons representing items of this type within diagrams.`
				},
				{
					name: $localize`Icon`,
					type: AssetTypeDetailFieldType.ICON,
					value: assetTypeModel?.IconStyle?.Icon,
					tooltip: $localize`Sets the icon representing items of this type within the summary page/search results.`
				}
			]);
		}

		this.addFieldsToCategory($localize`System Fields`, [
			{ name: 'UID', type: AssetTypeDetailFieldType.SYSTEM, value: assetTypeModel.uid },
			{ name: `Date Created`, type: AssetTypeDetailFieldType.DATE, value: assetTypeModel.CreatedOn },
			{ name: `Date Last Modified`, type: AssetTypeDetailFieldType.DATE, value: assetTypeModel.UpdatedOn },
			{ name: `Created By`, type: AssetTypeDetailFieldType.USER, value: { title: assetTypeModel.CreatedByName, value: assetTypeModel.CreatedByUid } },
			{ name: `Last Modified By`, type: AssetTypeDetailFieldType.USER, value: { title: assetTypeModel.UpdatedByName, value: assetTypeModel.UpdatedByUid } }
		]);
	}

	private fillParentRelationshipCategories(assetTypeModel: AssetTypeApiModel): void {
		switch (assetTypeModel.Class.ID) {
			case AssetTypeClass.BusinessAsset:
			case AssetTypeClass.TechnicalAsset:
				if (assetTypeModel.PredicateInverse) {
					this.addFieldsToCategory($localize`General`, [
						{
							name: 'Predicate to Parent',
							type: AssetTypeDetailFieldType.TEXT,
							value: assetTypeModel.PredicateInverse
						},
						{
							name: 'Auto Display Owner/Parent?',
							type: AssetTypeDetailFieldType.BOOL,
							value: assetTypeModel.AutoDisplayParent
						},
						{
							name: 'Edit Parent?',
							type: AssetTypeDetailFieldType.BOOL,
							value: assetTypeModel.CanEditParent
						}
					]);
				}
				break;
			case AssetTypeClass.Model:
			case AssetTypeClass.Policy:
				this.addFieldsToCategory($localize`General`, [
					{
						name: 'Predicate to Parent',
						type: AssetTypeDetailFieldType.TEXT,
						value: assetTypeModel.PredicateInverse
					}
				]);
				this.addFieldsToCategory($localize`General`, [
					{
						name: 'Maximum Depth',
						type: AssetTypeDetailFieldType.TEXT,
						value: assetTypeModel.HierarchyMaximumDepth
					}
				]);
				break;
		}
	}

	private fillCategories(assetTypeModel: AssetTypeApiModel): void {
		this.fillBasicCategories(assetTypeModel);

		switch (assetTypeModel.Class.ID) {
			case AssetTypeClass.BusinessAsset:
			case AssetTypeClass.TechnicalAsset:
				this.fillSynonyms(assetTypeModel);
				this.addFieldsToCategory($localize`General`, [
					{
						name: 'Use as Transformation?',
						type: AssetTypeDetailFieldType.BOOL,
						value: assetTypeModel.UseAsTransformation
					}
				]);
				break;
			case AssetTypeClass.Rule:
			case AssetTypeClass.Model:
			case AssetTypeClass.Policy:
				this.fillSynonyms(assetTypeModel);
				break;
			case AssetTypeClass.DiagramAsset:
				this.addFieldsToCategory($localize`General`, [
					{
						name: $localize`Description`,
						type: AssetTypeDetailFieldType.HTML,
						value: assetTypeModel.Description
					}
				]);
				this.addFieldsToCategory($localize`General`, [
					{
						name: 'Flow Object Type',
						type: AssetTypeDetailFieldType.TEXT,
						value: assetTypeModel.FlowObjectType
					},
					{
						name: $localize`Icon`,
						type: AssetTypeDetailFieldType.ICON,
						value: assetTypeModel?.IconStyle?.Icon,
						tooltip: $localize`Sets the icon representing items of this type within the summary page/search results.`
					}
				]);
				break;
		}
	}

	private fillSynonyms(assetTypeModel: AssetTypeApiModel) {
		this.synonyms.forEach((syn) => {
			this.addFieldsToCategory($localize`Grammatic Type Allocation`, [
				{
					name: syn.Name,
					type: AssetTypeDetailFieldType.BOOL,
					value: (assetTypeModel.SynonymAllocations && assetTypeModel.SynonymAllocations.some((x) => x.toLowerCase() === syn.Uid.toLowerCase()) ? true : false)
				}
			]);
		});
	}

	// disable complexity codacy issue
	// eslint-disable-next-line
	getAssetTypeClass(): AssetTypeClass {
		switch (this.assetTypeModel.Class.Value.toString()) {
			case `BusinessAsset`: return AssetTypeClass.BusinessAsset; 
			case `DiagramAsset`: return AssetTypeClass.DiagramAsset; 
			case `Group`: return AssetTypeClass.Group; 
			case `Model`: return AssetTypeClass.Model; 
			case `Policy`: return AssetTypeClass.Policy; 
			case `Reference`: return AssetTypeClass.Reference; 
			case `ReferenceItemType`: return AssetTypeClass.ReferenceItemType; 
			case `Rule`: return AssetTypeClass.Rule; 
			case `SemanticType`: return AssetTypeClass.SemanticType; 
			case `TechnicalAsset`: return AssetTypeClass.TechnicalAsset; 
			case `User`: return AssetTypeClass.User; 
		}
		return this.assetTypeModel.Class.Value as AssetTypeClass;
	}
}
