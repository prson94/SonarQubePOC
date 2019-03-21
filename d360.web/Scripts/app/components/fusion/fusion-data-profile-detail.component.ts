import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { AssetDataProfile } from "../../models/fusion.model";

@Component({
    selector: "d3s-fusion-data-profile-detail",
    templateUrl: "./fusion-data-profile-detail.component.html"
})
export class FusionDataProfileDetailComponent extends BaseComponent{
  
  
    @Input() assetDataProfile: AssetDataProfile=null;
  
}