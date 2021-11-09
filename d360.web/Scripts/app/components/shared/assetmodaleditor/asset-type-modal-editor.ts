import { Input, Component, EventEmitter, Output, OnChanges, SimpleChanges, OnInit, ViewChild, ElementRef, HostListener, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetType } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { Subscription } from 'rxjs';
import { CATCH_STACK_VAR } from '@angular/compiler/src/output/output_ast';
import { DynamicEditorComponent } from '../dynamicgrideditor/dynamic-editor.component';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-asset-type-modal-editor',
    templateUrl: './asset-type-modal-editor.html',
    providers: [AssetTypeService]
})

export class AssetTypeModalEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() isModalVisable: boolean = false;
    @Output() closeEvent = new EventEmitter;
    @Output() onSave = new EventEmitter;
    @Input() options: AssetType[] = [];
    assetType: AssetType = null;
    private assetTypeClass: any;
    editorOpen: boolean = false;
    private isModelLoading: boolean = false;
    private asSub: Subscription;
    savingInProgress: boolean = false;
    isValid: boolean = false;

    @ViewChild('definition', { static: false }) definition: ElementRef;
    @ViewChild('dynamicEditor', { static: false }) dynamicEditor: DynamicEditorComponent;

    constructor(
        private assetTypeService: AssetTypeService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef
    ) {
        super(settingsService);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (!changes.isModalVisable.isFirstChange() && (changes.isModalVisable.currentValue !== changes.isModalVisable.previousValue)) {
            this.load();
        }
    }
    
    @HostListener('document:keydown', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        window.setTimeout(() => this.validate(), 50);
    }

    ngOnInit(): void {
        this.load();
    }
    private load(): void{
        this.asSub = this.assetTypeService.getAssetTypesDetails().subscribe(types => {
            if (types.length > 0) {
                this.options = types.filter(x => x.UseAsTransformation === true);
            }
        });
    }
    updateAssetTypeEditor(): void {
        this.editorOpen = false; 
        window.setTimeout(() => {
            this.editorOpen = true;
            this.definition.nativeElement.style.maxHeight = Math.round(window.innerHeight / 2) + "px";
        }, 100);
    }

    confirm() {
        if (this.dynamicEditor) {
            this.savingInProgress = true;
            this.dynamicEditor.onSubmit();
        }
    }

    validate() {
        this.isValid = false;
        if (this.dynamicEditor && this.dynamicEditor.form) {
            if (this.dynamicEditor.form.valid)
                this.isValid = true;
            this.ref.markForCheck();
        }
    }

    savedItem(event) {
        this.savingInProgress = false;
        if (event.Success) {
            this.editorOpen = false; 
            this.assetType = null;
            this.assetTypeClass = null;
            this.isModelLoading = false;
            this.onSave.emit(event);
        }
    }

    cancel() {
        this.editorOpen = false;
        this.assetType = null;
        this.assetTypeClass = null;
        this.isModelLoading = false;
        if (this.asSub) {
            this.asSub.unsubscribe();
        }
        this.closeEvent.emit();
    }
}