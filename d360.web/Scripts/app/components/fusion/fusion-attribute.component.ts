import { Component, Input, EventEmitter } from "@angular/core";
import { BaseComponent } from "../shared/base.component";

@Component({
    selector:"d3s-fusion-attribute",
    templateUrl: "fusion-attribute.component.html"
})
export class FustionAttributeComponent extends BaseComponent {
    @Input() fusionId: number;

    @Input() selectedFusionAttributeTypeId: number;
    @Input() selectedFusionAttribute: any;
    @Input() initialFusionAttributeId: number;

    @Input() selectedFusionQueryAttributeTypeId: number;
    @Input() selectedFusionQueryAttribute: any;
    @Input() initialFusionQueryAttributeId: number;



}