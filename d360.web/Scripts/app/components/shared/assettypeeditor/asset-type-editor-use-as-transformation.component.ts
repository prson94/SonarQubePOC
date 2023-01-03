import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../base.component";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { RelationshipsService } from "../../../services/relationships.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-asset-type-editor-use-as-transformation',
    providers: [RelationshipsService],
    templateUrl: './asset-type-editor-use-as-transformation.component.html'

})
export class AssetTypeEditorUseAsTransformationComponent extends BaseComponent implements OnChanges, OnInit{
    
    
    @Input()
    UseAsTransformation: boolean;
    @Output()
    UseAsTransformationChange = new EventEmitter();
    @Input()
    assetTypeId: number = 0;
    initialValue: boolean;
    isRelationsExist : boolean =false;

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
            if (this.initialValue) {this.IsTransformPredicateExists();}
        }
    }

    IsTransformPredicateExists() {
        if (this.assetTypeId != null && this.assetTypeId !== 0) {
            this.relationshipsService.IsTransformPredicateExists(this.assetTypeId).subscribe((res) => {
                if (res) {
                    setTimeout(() => {
                        this.isRelationsExist = true;
                        this.UseAsTransformation = this.initialValue;
                        this.UseAsTransformationChange.emit(this.UseAsTransformation);
                    });
                }
            });
        }
    }

    tranformationChange($event) {
            this.UseAsTransformation = $event;
            this.UseAsTransformationChange.emit(this.UseAsTransformation);
    }
}