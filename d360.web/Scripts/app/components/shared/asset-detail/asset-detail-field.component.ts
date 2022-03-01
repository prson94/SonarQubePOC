import { Input, Component, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';
import { DetailField, DetailFieldType } from '../../../models/object-detail.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Router } from '@angular/router';
import { AssetService } from '../../../services/asset.service';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { GenericMessageService } from '../../../services/generic-message.service';
import { GenericMessageType } from '../../../models/generic-message.model';

@Component({
    selector: 'ig-asset-detail-field',
    templateUrl: './asset-detail-field.component.html',
    changeDetection: ChangeDetectionStrategy.Default,
    providers: [AssetService]
})

export class AssetDetailFieldComponent {
    @Input() field: DetailField;
    @Input() assetUid: string;
    @Input() tooltipAlign: string;
    @Input() isSidePanel: boolean = false;
    @Input() interceptLinkClick: boolean = false;
    @Output() tagsChanged = new EventEmitter<string>();

    readonly emptyValue: string = "---";
    readonly dateFormat: string = "d MMM yyyy";
    readonly dateTimeFormat: string = "d MMM yyyy HH:mm:ss";
    DetailFieldType = DetailFieldType;

    jsonValue: any = null;


    constructor(private router: Router,
        private assetService: AssetService,
        private ref: ChangeDetectorRef,
        private linkClickInterceptor: LinkClickInterceptor,
        private genericMessageService: GenericMessageService
    ) { }

    ngOnInit() {
        if ((this.field.DataType === 'date' || this.field.DataType === 'datetime') && isNaN(Date.parse(this.field.Value))) {
            this.field.Value = null;
        }
    }

    navigate(url: string, e: any, item = null) {
        if (this.interceptLinkClick) {
            this.linkClickInterceptor.sendEvent(e, this.field, SiteUrlHelpers.convertClassicUrl(url), item !== null ? this.field.Values.indexOf(item) : 0);
            return;
        }
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(url));
        if (e) {
            e.preventDefault();
        }
    }

    get shouldShowEmptyValue(): boolean {
        if (this.field == null) {
            return false;
        }

        return ((this.field.Value == null || this.field.Value === "") && this.field.ShowIfEmpty === true);

    }

    get fieldDataType(): string {
        if (this.field == null || this.field.DataType == null) {
            return null;
        }
        switch (this.field.DataType.toLowerCase()) {
            case 'text':
            case 'string':
                return 'text';
            case 'number':
            case 'decimal':
                return 'number';
            case 'bool':
            case 'boolean':
                return 'bool';
            default:
                return this.field.DataType.toLowerCase();
        }
    }

    get isUtcDate(): boolean {
        if (this.field
            && this.field.DataType
            && (this.field.DataType === "date" || this.field.DataType === "datetime")
            && this.field.Value
            && this.field.Value.endsWith('Z')) {
            return true;
        }
        return false;
    }

    get isArrayValue(): boolean {
        return this.field != null
            && this.field.Values
            && this.field.Values.length > 0;
    }

    get valueCount(): number {
        if (this.isArrayValue) {
            return this.field.Values.length;
        } else if (this.field == null) {
            return 0;
        } else {
            return 1;
        }
    }

    //#region Formatted field values

    get formattedNumber(): string {
        return this.field.Value !== "" && this.field.Value != null ? Number(this.field.Value).toLocaleString() : "";
    }

    get linkUrl(): string {
        if (this.field == null || this.field.Value.indexOf("|") === -1) {
            return null;
        }
        let index = this.field.Value.indexOf("|");

        return this.field.Value.substring(index + 1);
    }

    get linkName(): string {
        if (this.field == null || this.field.Value.indexOf("|") === -1) {
            return null;
        }
        let index = this.field.Value.indexOf("|");
        if (index === 0) {
            return this.linkUrl;
        }
        else {
            return this.field.Value.split("|")[0];
        }
    }

    get json(): any {
        if (this.jsonValue != null) {
            return this.jsonValue;
        }
        try {
            this.jsonValue = JSON.parse(this.field.Value);
            return this.jsonValue;
        } catch (err) {
            return "Error";
        }
    }

    private onTagsChanged(tags) {
        this.genericMessageService.sendMessage({
            uid: this.assetUid,
            messageType: GenericMessageType.Tags,
            data: tags
        });
    }

    getDataLinkType(data) {
        if (data && data.TooltipType && data.TooltipType === "Resource") {
            return "resource";
        }
        return "asset";
    }
}

