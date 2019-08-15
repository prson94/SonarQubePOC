import { Input, Component, EventEmitter, Output, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetService } from "../../../services/asset.service";
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-asset-delete-editor',
    templateUrl: './asset-delete-editor.component.html',
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetDeleteEditorComponent extends BaseComponent {
    @Input() displayValue: string;
    @Input() uid: string;
    @Input() assetTypeUid: string;

    @Output() onCancel = new EventEmitter();
    @Output() onDeleted = new EventEmitter();

    theDeleteCallback: Function;

    constructor(
        private assetService: AssetService,
        private messagesService: MessagesObservableService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super();

        this.theDeleteCallback = this.deleteAsset.bind(this);        
    }
    
    public deleteAsset(id:number): void {
        this.assetService.deleteAsset(this.assetTypeUid, this.uid)
            .subscribe(
                result => {
                    this.showMessageForResult(this.messagesService, result);
                    this.onDeleted.emit();
                    this.changeDetectorRef.markForCheck();
                }
        )
    }

}