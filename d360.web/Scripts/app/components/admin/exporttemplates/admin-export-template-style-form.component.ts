import { FormEvents } from "../../../models/form.model";
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { ExportTemplateStyle, ExportViewType } from "../../../models/export-template.model";
import { SelectItem } from "primeng/api";
import { ExportTemplateService } from "../../../services/export-template.service";
import { clone } from "lodash-es";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-admin-export-template-style-form',
    templateUrl: 'admin-export-template-style-form.component.html'
})
export class AdminExportTemplateStyleFormComponent extends BaseComponent implements OnChanges, FormEvents {
    @Input() mode: string;
    @Input() exportViewType: ExportViewType;
    @Input() selectedStyle: ExportTemplateStyle;
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onError = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Input() templateId: number;

    labelSave = $localize`Save`;
    labelCancel = $localize`Cancel`;

    private model: ExportTemplateStyle;
    selections: SelectItem[] = [{ label: $localize`Column`, value: "Column" }, { label: "Row", value: "Row" }];

    constructor(
        private exportTemplateService: ExportTemplateService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.model = {
            SelectionType: "Row",
            BgColor: "#FFFFFF",
            TextColor: "#000000",
            Column: -1,
            Row: 1,
            ID: 0,
            AssetTypeExportTemplateID: 0,
            IsBold: false
        };
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.model.AssetTypeExportTemplateID = changes["templateId"].currentValue;
        if (changes["mode"].currentValue === $localize`Edit` && changes["selectedStyle"].currentValue) {
            this.model = clone(changes["selectedStyle"].currentValue);
        }
        this.model.SelectionType = this.exportViewType.toString() !== String(ExportViewType[ExportViewType.Pivot]) ? "Header" : this.model.Column === -1 ? "Row" : "Column";

        if (this.exportViewType.toString() === String(ExportViewType[ExportViewType.Pivot]))
            {this.selections = [
                { label: $localize`Column`, value: "Column" },
                { label: $localize`Row`, value: "Row" }];}
        else
            {this.selections = [{ label: $localize`Header`, value: "Header" }];}

    }
    selectionChange() {
        if (this.model.SelectionType === "Row" || this.model.SelectionType === "Header") {
            this.model.Column = -1;
            this.model.Row = 1;
        } else {
            this.model.Column = 1;
            this.model.Row = -1;
        }
    }
    save() {
        this.exportTemplateService.saveExportTemplateStyle(this.model).subscribe((result) => {
            if (this.model.ID) {
                this.messagesService.showInfoMessage($localize`Success`, $localize`Style updated successfully`);
            } else {
                this.messagesService.showInfoMessage($localize`Success`, $localize`Style added successfully`);
            }
            //default
            this.model = { SelectionType: "Row", BgColor: "#FFFFFF", TextColor: "#000000", Column: null, Row: null, ID: 0, AssetTypeExportTemplateID: 0, IsBold: false };
            this.onSuccess.emit(null);
        });
    }
} 