import { Component, Input, EventEmitter, OnChanges, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../shared/base.component";
import { ResponsibilityTypeRelationPermission } from "../../models/responsibility-type.model";

@Component({
    selector:"d3s-fusion-attribute",
    templateUrl: "fusion-attribute.component.html"
})
export class FusionAttributeComponent extends BaseComponent implements OnChanges {

    @Input() fusionId: number;

    @Input() selectedFusionAttributeTypeId: number;
    @Input() selectedFusionAttribute: any;
    @Input() initialFusionAttributeId: number;

    @Input() selectedFusionQueryAttributeTypeId: number;
    @Input() selectedFusionQueryAttribute: any;
    @Input() initialFusionQueryAttributeId: number;
    @Input() objectPermissions: ResponsibilityTypeRelationPermission[] = [];

    ngOnChanges(changes: SimpleChanges): void {
        this.permissions = this.objectPermissions;
    }


}