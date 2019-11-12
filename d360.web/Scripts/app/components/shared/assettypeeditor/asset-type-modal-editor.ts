import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges, OnInit, ViewChild, ElementRef } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetTypeClass, AssetTypeEditorModel, AssetType } from '../../../models/asset.model';
import { ApiResult } from '../../../models/apiresult.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Subscription } from 'rxjs';
import { CATCH_STACK_VAR } from '@angular/compiler/src/output/output_ast';

declare var CompanySettings: any;

@Component({
    selector: 'd3s-asset-type-modal-editor',
    templateUrl: './asset-type-modal-editor.html',
    providers: [AssetTypeService],
})

export class AssetTypeModalEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() isModalVisable: boolean = false;
    @Output() closeEvent = new EventEmitter;
    @Input() options: AssetType[] = [];
    private assetType: AssetType = null;
    private assetTypeClass: any;
    private editorOpen: boolean = false;
    private isModelLoading: boolean = false;
    private asSub: Subscription;

    @ViewChild('definition', { static: false }) definition: ElementRef;

    constructor(private assetTypeService: AssetTypeService, private messagesService: MessagesObservableService) {
        super();
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (!changes.isModalVisable.isFirstChange() && (changes.isModalVisable.currentValue !== changes.isModalVisable.previousValue)) {
            this.load();
        }
    }

    ngOnInit(): void {
        this.load();
    }
    private load(): void{
        this.asSub = this.assetTypeService.getAssetTypes().subscribe(types => {
            if (types.length > 0) {
                this.options = types.filter(x => x.UseAsTransformation === true);
            }
        });
    }
    updateAssetTypeEditor(): void {
        this.editorOpen = false;
        window.setTimeout(() => {
            this.editorOpen = true;
            this.definition.nativeElement.style.width = Math.round(window.innerWidth / 2) + "px";
            this.definition.nativeElement.style.maxHeight = Math.round(window.innerHeight / 2) + "px";
        }, 100);
    }

    cancel() {
        this.editorOpen = false;
        this.assetType = null;
        this.assetTypeClass = null;
        this.isModelLoading = false;
        this.asSub.unsubscribe();
        this.closeEvent.emit();
    }
}