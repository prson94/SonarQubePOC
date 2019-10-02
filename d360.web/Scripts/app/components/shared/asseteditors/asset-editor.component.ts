import { Input, Component, EventEmitter, Output, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetService } from "../../../services/asset.service";
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-asset-editor',
    templateUrl: './asset-editor.component.html',
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetEditorComponent extends BaseComponent {

    theCloseCallback: Function;
    theSaveCallback: Function;

    constructor(
        private assetService: AssetService,
        private messagesService: MessagesObservableService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super();

        this.theSaveCallback = this.saveAsset.bind(this);        
    }

    private saveAsset($event) {
        console.log("saving");
        console.log($event);
    }

}