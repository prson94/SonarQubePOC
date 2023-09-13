import { Component, EventEmitter, Input, OnChanges, Output, SimpleChange } from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
import { AssetTypeClass } from "../../../../models/asset.model";
import { AssetTypeService } from "../../../../services/asset-type.service";
import { AssetService } from "../../../../services/asset.service";
import { MessagesObservableService } from "../../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../../services/settings.service";
import { StateService } from "../../../../services/state.service";
import { BaseComponent } from "../../../shared/base.component";

/*global $localize*/

@Component({
	selector: "d3s-configuration-asset-type-delete-page",
	templateUrl: './configuration-asset-type-delete-page.component.html',
	styleUrls: ['configuration-asset-type-delete-page.component.less']
})
export class ConfigurationAssetTypeDeletePageComponent extends BaseComponent implements OnChanges {
	@Input() uid: string;
	@Output() onClose = new EventEmitter();

	assetType: { Name: string };
	assetsCount?: number;

	loadingCounter = 0;
	isModalVisible: boolean = false;
	isConfirmed: boolean = false;
	message: string = "";

	titleAssetType = $localize`Delete Asset Type`;
	titleReferenceList = $localize`Delete Reference List`;
	confirmAssetType = $localize`Are you sure you want to delete this asset type?`;
	confirmReferenceList = $localize`Are you sure you want to delete this reference list?`;
	title = this.titleAssetType;
	confirm = this.confirmAssetType;

	constructor(
		private route: ActivatedRoute,
		private stateService: StateService,
		private assetTypeService: AssetTypeService,
		private assetsService: AssetService,
		protected messagesService: MessagesObservableService,
		settingsService: CompanySettingsService,
		private router: Router) {
		super(settingsService);
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes['uid']) {
			if (changes['uid'].previousValue !== changes['uid'].currentValue) {
				// object has changed
				this.isConfirmed = false;
				if (this.uid) {
					this.loadAssetType(this.uid);
					this.loadCount(this.uid);
					this.isModalVisible = true;
				}
				else {

					this.isModalVisible = false;
				}
			}
		}
	}

	cancel(reloadList: boolean = false) {
		this.isConfirmed = false;
		this.onClose.emit(reloadList);
	}

	async loadAssetType(uid: string) {
		this.loadingCounter++;
		try {
			const assetType = await this.assetTypeService.GetAssetTypeByUid(uid).toPromise();
			this.title = assetType.Class.ID === AssetTypeClass.Reference ? this.titleReferenceList : this.titleAssetType;
			this.confirm = assetType.Class.ID === AssetTypeClass.Reference ? this.confirmReferenceList : this.confirmAssetType;
			if (uid === this.uid) {
				this.assetType = assetType;
				this.formatMessage();
			}
		} finally {
			this.loadingCounter--;
		}
	}

	async loadCount(uid: string) {
		this.loadingCounter++;
		try {
			const assetsCount = (await this.assetsService.getAssetCountOfArtifactTypeUid(uid).toPromise()).count;
			if (uid === this.uid) {
				this.assetsCount = assetsCount;
				this.formatMessage();
			}
		} finally {
			this.loadingCounter--;
		}
	}

	formatMessage() {
		const name = this.assetType.Name;
		const undoneMsg = $localize`Please note that this operation cannot be undone.`;
		const checkBoxMsg = $localize`Please check this box if you would like to continue.`;
		this.isConfirmed = false;
		if (this.assetsCount === 0) {
			this.message = `"${name}" contains 0 assets.`;
			this.isConfirmed = true;
			return;
		}
		else if (this.assetsCount > 1) {
			this.message = `"${name}" contains ${this.assetsCount} assets that will also be deleted.`;
		}
		else {
			this.message = `"${name}" contains ${this.assetsCount} asset that will also be deleted.`;
		}
		this.message += " " + undoneMsg;
		this.message += " " + checkBoxMsg;
	}

	deleteInProgress: boolean = false;

	delete() {
		this.deleteInProgress = true;
		this.assetTypeService.deleteSingleAssetType(this.uid).subscribe((result) => {
			this.deleteInProgress = false;
			result.title = $localize`Success` + "!";
			this.showMessageForResult(this.messagesService, result, $localize`Item successfully removed` + ".");
			this.cancel(true);
		});

	}
}
