import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../base.component";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { RelationshipsService } from "../../../services/relationships.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-asset-type-editor-use-as-transformation',
    providers: [RelationshipsService],
    template: `
<ng-container>
    <div class="FieldName" i18n>Use As Transformation?</div>
    <div class="row">
        <div class="col l1 m2 s12">
            <input pCheckbox type="checkbox" [(ngModel)]="UseAsTransformation" [disabled]="isRelationsExist" (ngModelChange)="tranformationChange($event)" />
        </div>
        <div class="col l11 m10 s12">
            <div class="FieldInstructions" i18n>If enabled, assets of this type may be used as a connecting asset to form lineage relationships.</div>
        </div>
    </div>
</ng-container>
`

})
export class AssetTypeEditorUseAsTransformationComponent extends BaseComponent implements OnChanges, OnInit{
    
    
    @Input()
    UseAsTransformation: boolean;
    @Output()
    UseAsTransformationChange = new EventEmitter();
    @Input()
    assetTypeId: number = 0;
    initialValue: boolean;
    isRelationsExist : boolean =false

    constructor(
        private messagesService: MessagesObservableService,
        private relationshipsService: RelationshipsService,
        protected settingsService: CompanySettingsService) {
        super(settingsService);
    } 

    ngOnInit() {
        this.IsTransformPredicateExists();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (typeof changes['UseAsTransformation'] !== "undefined" && changes['UseAsTransformation'].firstChange) {
            this.initialValue = changes['UseAsTransformation'].currentValue;
            if (this.initialValue) this.IsTransformPredicateExists();
        }
    }

    IsTransformPredicateExists() {
        if (this.assetTypeId != undefined && this.assetTypeId != 0) {
            this.relationshipsService.IsTransformPredicateExists(this.assetTypeId).subscribe(res => {
                if (res) {
                    setTimeout(() => {
                        this.isRelationsExist = true;
                        this.UseAsTransformation = this.initialValue;
                        this.UseAsTransformationChange.emit(this.UseAsTransformation);
                    })
                }
            });
        }
    }

    tranformationChange($event) {
            this.UseAsTransformation = $event;
            this.UseAsTransformationChange.emit(this.UseAsTransformation);
    }
}