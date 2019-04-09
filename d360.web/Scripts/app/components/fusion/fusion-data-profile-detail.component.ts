import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { AssetDataProfile } from "../../models/fusion.model";
import { FusionAttributeService } from "../../services/fusion-attribute.service";

@Component({
    selector: "d3s-fusion-data-profile-detail",
    templateUrl: "./fusion-data-profile-detail.component.html",
    providers:[FusionAttributeService]
})
export class FusionDataProfileDetailComponent  extends BaseComponent implements OnChanges{
  
    top10Values: any
    showTop10Values: boolean = false;
    
    assetDataProfile: AssetDataProfile = null;
    @Input() profileId: number = -1;

    constructor(private fusionAttributeService: FusionAttributeService) {
        super();
    }
    ngOnChanges(changes: SimpleChanges): void {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionAttributeService.getAssetDataProfile(this.profileId).subscribe(
            item => {
                this.assetDataProfile = item;
                this.top10Values = JSON.parse(this.assetDataProfile.Top10Values)
                this.isLoading = false;
            }
        );
    }
  
}