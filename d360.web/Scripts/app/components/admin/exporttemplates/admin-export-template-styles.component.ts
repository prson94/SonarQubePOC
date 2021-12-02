import { Component, Input, OnChanges, SimpleChanges, OnInit } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { ExportTemplateService } from "../../../services/export-template.service";
import { ExportTemplateStyle, ExportViewType } from "../../../models/export-template.model";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-admin-export-template-styles',
    template: `
  <div *ngIf="!showEditor && !showDelete" class="row">
                <div class="tile tile-detail">
                    <header *ngIf="!showEditor">Styling Rules
                        <d3s-tile-actions [hasAdd]="canAdd()" (addClick)="mode='Add';showEditor=true;"></d3s-tile-actions>                            
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <p-table #dt [value]="styleRules" selectionMode="single" [metaKeySelection]="true" [sortOrder]="1" [pageLinks]="3" [paginator]="true" [rows]="10" >
                            <ng-template pTemplate="header">
                                <tr>
                                    <th>
                                        Name
                                   </th>
                                    <th>
                                        Style
                                    </th>
                                    <th style="width: 40px"></th>
                                    <th style="width: 40px"></th>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="body" let-item>
                                <tr  [pSelectableRow]="item">
                                    <td>
                                        <span *ngIf="isPivot() && item.Column ==-1">Row {{item.Row}}</span>
                                        <span *ngIf="isPivot() && item.Row ==-1">Column {{item.Column}}</span>
                                         <span *ngIf="!isPivot()">Header</span>
                                    </td>
                                    <td style="padding: 5px">
                                        <span [ngStyle]="getRowStyles(item)">Style Sample</span>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selectedStyle=item;mode='Edit';showEditor=true"><i class="fa fa-pencil"></i></a>
                                        </div>
                                    </td>
                                    <td>
                                        <div class="RowTools">
                                            <a style="cursor:pointer;" (click)="selectedStyle=item;showDelete=true"><i class="fa fa-trash-o"></i></a>
                                        </div>
                                    </td>
                                </tr>
                            </ng-template>
                            <ng-template pTemplate="summary">
                                <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            </ng-template>
                        </p-table>
                    </span>                            
                </div>
        </div>  
          <div *ngIf="showEditor && !showDelete" class="row">
                <div class="tile tile-detail">
                   <d3s-admin-export-template-style-form [mode]="mode" [exportViewType]="exportViewType" [templateId]="templateId" [selectedStyle]="selectedStyle" (onSuccess)="showEditor=false;load()" (onCancel)="showEditor=false;"></d3s-admin-export-template-style-form>
                </div>
        </div>
        <div class="tile tile-detail" *ngIf="showDelete">
            <d3s-delete-form 
                [callback]="theDeleteCallback"
                [itemId]="selectedStyle?.ID"
                [method]="'callback'"
                [prompt]="'Are you sure you want to delete the selected item?'"                                         
                (onCancel)="showDelete=false">
            </d3s-delete-form> 
        </div>
`
})
export class AdminExportTemplateStylesComponent extends BaseComponent implements OnInit, OnChanges{
    styleRules: ExportTemplateStyle[]=[];
    selectedStyle: any;
    showEditor: boolean = false;
    showDelete: boolean = false;
    @Input()
    templateId: number;
    @Input()
    exportViewType: ExportViewType;
    mode: string;
    theDeleteCallback: Function;

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
        return (this.exportViewType && this.exportViewType.toString() == ExportViewType[ExportViewType.Pivot]);        
    }

    ngOnInit(): void {
        this.load();
    }

    canAdd(): boolean {           
        var retval = false;
        if (this.exportViewType) {
            if (this.exportViewType.toString() == ExportViewType[ExportViewType.Pivot])
                retval = true;
            else if ((this.exportViewType.toString() == ExportViewType[ExportViewType.Grouped] || this.exportViewType.toString() == ExportViewType[ExportViewType.None])
                && (this.styleRules == null || this.styleRules.length == 0))
                retval = true;
        }
        return retval
    }

    private getRowStyles(item:ExportTemplateStyle): any {
        let styles = {
            'background-color': item.BgColor,
            'font-weight': item.IsBold ? 'bold' : 'normal',
            'color':item.TextColor,
            'align': 'left',
            'padding': '10px',
            'width': '100px',
            'height': '75px',
            'border-radius': '10px',

        };

        return styles;
    }

    public deleteTemplateStyle(id: number) {
        this.exportTemplateService.deleteExportTemplateStyle(id).subscribe(result => {
            this.messagesService.showInfoMessage('Success', 'Style deleted successfully');
            this.showDelete = false;
            this.load();
        });
    }  

    private load() {
        this.isLoading = true;
        if (!this.templateId || this.templateId == 0) {
            this.isLoading = false;
            return;
        }
        this.exportTemplateService.getExportTemplateStyles(this.templateId).subscribe(result => {
            if (this.exportViewType != ExportViewType.Pivot)
                this.styleRules = result.filter(x => x.Column == -1);
            else
                this.styleRules = result;
            this.isLoading = false;
        });
    }
}