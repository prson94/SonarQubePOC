import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../services/asset-type.service";
import { FieldDefinitionComponent } from "../../../../shared/fielddefinition/field-definition.component";

type FieldTileSettings = Pick<
    FieldDefinitionComponent,
	'supportsPrimaryFilterOption' | 'showDisplayInColumn' | 'allowSingleSegmentPath' | 'showAddToSearch'
>;

@Component({
    selector: "d3s-configuration-issue-type-fields-page",
	templateUrl: './configuration-issue-type-fields-page.component.html'
})
export class ConfigurationIssueTypeFieldsPageComponent {
	issueTypeUid: string;

    constructor(
        private route: ActivatedRoute,
        private assetTypeService: AssetTypeService) {
    }

    ngOnInit() {
		this.route.params.subscribe((params) => {
			this.issueTypeUid = params["uid"];
            //this.loadAssetType(this.uid);
        });
    }
}
