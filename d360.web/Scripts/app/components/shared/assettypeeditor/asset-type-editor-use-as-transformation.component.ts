import { Component, Input, Output, EventEmitter, OnInit } from "@angular/core";
import { BaseComponent } from "../base.component";
@Component({
    selector: 'd3s-asset-type-editor-use-as-transformation',
    template: `
<ng-container>
    <div class="FieldName">Use As Transformation?</div>
    <div class="row">
        <div class="col l1 m2 s12">
            <input pCheckbox type="checkbox" [ngModel]="UseAsTransformation" (ngModelChange)="UseAsTransformation=$event;UseAsTransformationChange.emit(UseAsTransformation)" />
        </div>
        <div class="col l11 m10 s12">
            <div class="FieldInstructions">If enabled, assets of this type may be used as a connecting asset to form lineage relationships.</div>
        </div>
    </div>
</ng-container>
`

})
export class AssetTypeEditorUseAsTransformationComponent extends BaseComponent{
    
    @Input()
    UseAsTransformation: boolean;
    @Output()
    UseAsTransformationChange = new EventEmitter();
  

}