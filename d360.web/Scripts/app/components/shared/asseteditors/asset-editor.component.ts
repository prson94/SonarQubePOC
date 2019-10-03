import { Input, Component, EventEmitter, Output, ChangeDetectorRef, ChangeDetectionStrategy, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetService } from "../../../services/asset.service";
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { TreeNode } from '@angular/router/src/utils/tree';
import { AssetEditorModel } from '../../../models/asset.model';

@Component({
    selector: 'd3s-asset-editor',
    templateUrl: './asset-editor.component.html',
    providers: [AssetService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class AssetEditorComponent extends BaseComponent implements OnChanges {
    @Input() selectedItem: any;
    @Input() editorObjectType: string = '';
    @Input() assetTypeUid: number;
    @Input() parentId: number;
    @Input() editorTitle: string = '';
    @Input() assetTypeID: number;

    private editorObjectID: number = -1;
    private editorSelection: any = {};

    @Output() onClose: EventEmitter<any> = new EventEmitter();
    @Output() onSave: EventEmitter<any> = new EventEmitter();

    constructor(
        private assetService: AssetService,
        private messagesService: MessagesObservableService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super();
    }

    ngOnChanges(changes: SimpleChanges) {
        this.loadRecord();
    }

    loadRecord() {
        if (this.selectedItem && this.selectedItem.data) {
            this.editorObjectID = this.selectedItem.data.ID;
            this.editorSelection = this.selectedItem;
        }

        this.changeDetectorRef.markForCheck();
    }

    private saveAsset($event) {
        let uid = $event.item.Uid;
        let parentUid = $event.item.ParentUid;

        let fields: any = {};
        var props = Object.keys($event.item);
        props.filter(x => x != 'Uid' && x != 'ParentUid' && x != 'ParentID' && x != 'TaxonomyTypeID').forEach(p => {
            var value = $event.item[p];
            if (!value) {
                value = '';
            }
            fields[p] = value;
        });

        let model = new AssetEditorModel();
        model.Fields = fields;
        if (uid)
            model.Uid = uid;

        if (parentUid && parentUid.length == 36)
            model.ParentUid = parentUid;

        this.assetService.saveAsset(this.assetTypeUid.toString(), model)
            .subscribe(res => {
                if (res.Success) {
                    let msg = model.Uid ? 'Successfully updated' : 'Successfully added';
                    this.showMessageForApiResult(this.messagesService, res, msg);
                    this.onSave.emit($event);
                }
                else {
                    this.showMessageForApiResult(this.messagesService, res);
                }
                
            });
    }

    private close() {
        this.editorObjectID = -1;
        this.onClose.emit();
    }
}