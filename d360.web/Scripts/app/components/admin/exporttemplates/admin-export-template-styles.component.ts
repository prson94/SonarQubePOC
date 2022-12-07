import { Component, Input, OnChanges, OnInit, SimpleChanges } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { ExportTemplateService } from "../../../services/export-template.service";
import { ExportTemplateStyle, ExportViewType } from "../../../models/export-template.model";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-admin-export-template-styles',
    templateUrl: 'admin-export-template-styles.component.html'
})
export class AdminExportTemplateStylesComponent extends BaseComponent implements OnInit, OnChanges {
    styleRules: ExportTemplateStyle[] = [];
    selectedStyle: any;
    showEditor: boolean = false;
    showDelete: boolean = false;
    @Input()
    templateId: number;
    @Input()
    exportViewType: ExportViewType;
    mode: string;
    theDeleteCallback: Function;
    deleteMss = $localize`Are you sure you want to delete the selected item?`;

    constructor(
        private exportTemplateService: ExportTemplateService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
        this.theDeleteCallback = this.deleteTemplateStyle.bind(this);
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.showEditor = false;
        this.showDelete = false;
        this.load();
    }

    isPivot(): boolean {
        return (this.exportViewType && this.exportViewType.toString() === String(ExportViewType[ExportViewType.Pivot]));
    }

    ngOnInit(): void {
        this.load();
    }

    canAdd(): boolean {
        var retval = false;
        if (this.exportViewType) {
            if (this.exportViewType.toString() === String(ExportViewType[ExportViewType.Pivot]))
                {retval = true;}
            else if ((this.exportViewType.toString() === String(ExportViewType[ExportViewType.Grouped]) || this.exportViewType.toString() === String(ExportViewType[ExportViewType.None]))
                && (this.styleRules == null || this.styleRules.length === 0))
                {retval = true;}
        }
        return retval;
    }

    private getRowStyles(item: ExportTemplateStyle): any {
        const styles = {
            'background-color': item.BgColor,
            'font-weight': item.IsBold ? 'bold' : 'normal',
            'color': item.TextColor,
            'align': 'left',
            'padding': '10px',
            'width': '100px',
            'height': '75px',
            'border-radius': '10px',

        };

        return styles;
    }

    public deleteTemplateStyle(id: number) {
        this.exportTemplateService.deleteExportTemplateStyle(id).subscribe((result) => {
            this.messagesService.showInfoMessage($localize`Success`, $localize`Style deleted successfully`);
            this.showDelete = false;
            this.load();
        });
    }

    private load() {
        this.isLoading = true;
        if (!this.templateId || this.templateId === 0) {
            this.isLoading = false;
            return;
        }
        this.exportTemplateService.getExportTemplateStyles(this.templateId).subscribe((result) => {
            if (this.exportViewType !== ExportViewType.Pivot)
                {this.styleRules = result.filter((x) => x.Column === -1);}
            else
                {this.styleRules = result;}
            this.isLoading = false;
        });
    }
}