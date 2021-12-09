import { Input, Component, EventEmitter, Output, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { AssetService } from "../../../services/asset.service";
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../services/settings.service';

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
    descendantsMessage: string = "";
    isFormLoading: boolean = false;

    constructor(
        private assetService: AssetService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private changeDetectorRef: ChangeDetectorRef
    ) {
        super(settingsService);

        this.theDeleteCallback = this.deleteAsset.bind(this);
    }

    ngOnInit() {
        var params: any = { _onlyTotal: true };
        this.isFormLoading = true;
        this.assetService.getAssetDescendants(this.uid, params)
            .subscribe(
                (result) => {
                    let descendantsCount = result.total;
                    this.descendantsMessage = '';
                    if (descendantsCount > 0) {
                        this.descendantsMessage = `The selected asset contains <b>${descendantsCount}</b> descendants that will be deleted. This action cannot be undone. Please check the box to continue.`;
                    }
                    this.isFormLoading = false;
                    this.changeDetectorRef.markForCheck();
                }
            )
    }

    private getDisplayValue(): string {
        if (this.displayValue && this.displayValue !== "ERROR:KEY_FIELDS_NULL") {
            return `[${this.displayValue}]`;
        }
        return null;
    }

    public getPrompt(): string {
        const value = this.getDisplayValue() ?? "the selected item";
        return `Are you sure you want to delete ${value} ?`;
    }

    public deleteAsset(id: number): void {
        this.assetService.deleteAsset(this.assetTypeUid, this.uid)
            .subscribe(
                (result) => {
                    this.showMessageForApiResults(this.messagesService, result, `${this.displayValue} successfully deleted`, true);
                    this.onDeleted.emit();
                    this.changeDetectorRef.markForCheck();
                }
            )
    }

}