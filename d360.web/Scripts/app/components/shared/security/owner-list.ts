import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, SimpleChange, ViewChild } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { Router } from "@angular/router";
import { orderBy } from "lodash-es";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";
import { SecurityService } from "../../../services/security.service";
import { AssetOwnerModel, CreateSecurityPolicyOverride } from "../../../models/security.model";
import { Table } from "primeng/table";

@Component({
	selector: "owner-list",
	templateUrl: "./owner-list.html",
	providers: [SecurityService],
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class OwnerList extends BaseComponent implements OnChanges {
    @Input() assetUid: string;
    @Input() showRowTools: boolean = true;

    items = new Array<AssetOwnerModel>();
	selectedItem = new AssetOwnerModel();
	newItem = new CreateSecurityPolicyOverride();

	simpleFilterValue: string = '';

    isEditing = false;
    isDeleting = false;
    isAdding = false;

	@ViewChild('dt', { static: false }) dataTable: Table;

	sidePanelStorageKey(): string {
		return `AssetOwners_SidePanel_${this.assetUid}`;
	}

    constructor(
		private securityService: SecurityService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private router: Router,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnInit() {
        if (!this.isLoading) {
            this.load();
        }
    }

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (changes && changes.assetUid && changes.assetUid.currentValue !== changes.assetUid.previousValue) {
			this.load();
		}
    }

    load(): void {
        this.isLoading = true;
        this.ref.markForCheck();
        
        if (this.assetUid == null) {
            return;
        }
        
		this.securityService.getOwnersByAsset(this.assetUid)
            .subscribe((data) => {

				data.forEach((r) => {
					const menuItems = [
						{ title: "Edit", callback: () => { this.edit(r); }, visible: this.showEdit(r) },
						{ title: "Remove", callback: () => { this.delete(r); }, visible: this.hasDeleteResponsibilitiesPermissions() }
					];
					r.MenuItems = menuItems;
				});
				this.items = data;
				this.selectedItem = this.items[0];
                this.isLoading = false;
                this.ref.markForCheck();
            });

		this.objectPermission = {
			AddResponsibilities: true,
			AddAsset: true,
			AddRelationships: true,
			DeleteAsset: true,
			EditAsset: true,
			ReadAsset: true,
			DeleteRelationships: true,
			DeleteResponsibilities: true,
			EditRelationships: true,
			EditResponsibilities: true,
			ReadRelationships: true,
			ReadResponsibilities: true
		};

		//this.securityService.getAssetPermissions(this.assetUid)
		//	.subscribe((p) => {
		//	this.objectPermission = p;
		//});
    }

	edit(item): void {  
        this.selectedItem = item;
        this.isEditing = true;
	}

	delete(item): void {
		this.selectedItem = item;
		this.isDeleting = true;
	}

    canEdit(item: AssetOwnerModel): boolean {
        return item != null && item.isOverride && this.hasModifyResponsibilitiesPermissions();
    }

	showEdit(item: AssetOwnerModel): boolean {
        return this.showRowTools && this.canEdit(item);
    }

    add(): void {
		this.newItem = new CreateSecurityPolicyOverride();        
        this.isAdding = true;
    }

    
    navigate(url: string) {
		this.router.navigateByUrl(this.federateUrl(url));
    }

    onDoubleClick(item: any) {
        if (this.canEdit(item)) {
            this.edit(item);
        }
    }

    columnSort(event) {
        //event.field = Field to sort
        //event.order = Sort order, 1 ascending , -1 descending                        
		this.items = orderBy(this.items, [(item) => item[event.field] ? item[event.field].toLowerCase() : item[event.field]], [event.order === -1 ? "desc" : "asc"]);
    }

    deleteMessage() {
        return $localize`Are you sure you want to delete the ${this.selectedItem.roleName} - ${this.selectedItem.securityName}?`;
    }
}