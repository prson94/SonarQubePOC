import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnDestroy } from "@angular/core";
import { Subscription } from "rxjs";
import { Breadcrumb } from "../../../../models/breadcrumb.model";
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { AssetTypeService } from "../../../../services/asset-type.service";
import { isEmpty } from 'lodash-es';

/*global $localize*/

@Component({
	selector: "d3s-reference-item-type-header",
	templateUrl: './reference-header.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReferenceItemTypeHeaderComponent implements OnChanges, OnDestroy {
	@Input() uid: string;

	public title: string = $localize`Reference Lists`;
	private name: string = null;
	private subscription: Subscription;

	constructor(private assetTypeService: AssetTypeService,
		private headerBreadcrumbService: HeaderBreadcrumbService,
		private cdRef: ChangeDetectorRef
	) {
	}

	get icon() {
		return 'fa-list-alt';
	}

	get header() {
		return this.name ?? this.title;
	}

	get breadcrumbs() {
		const breadcrumbs: Breadcrumb[] = [];

		breadcrumbs.push(
			new Breadcrumb(this.title, `reference`)
		);

		if (!isEmpty(this.uid) && !isEmpty(this.name)) {
			breadcrumbs.push(new Breadcrumb(this.name, `reference/${this.uid}/details`));
		}

		return breadcrumbs;
	}

	async ngOnChanges() {
		this.title = await this.headerBreadcrumbService.getFolderTitle('#Reference');
		if (!isEmpty(this.uid)) {
			this.load();
		} else {
			this.name = null;
		}
		this.cdRef.markForCheck();
	}

	ngOnDestroy() {
		this.subscription?.unsubscribe();
	}

	public load(): void {
		if (this.subscription) {
			this.subscription.unsubscribe();
		}
		this.subscription = this.assetTypeService.GetAssetTypeByUid(this.uid).subscribe((res) => {
			this.name = res.Name;
			this.cdRef.markForCheck();
		});
	}
}
