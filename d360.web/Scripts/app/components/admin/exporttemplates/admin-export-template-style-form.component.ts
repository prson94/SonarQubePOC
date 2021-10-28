import { FormEvents } from "../../../models/form.model";
import { Input, Output, Component, EventEmitter, OnChanges, SimpleChanges, OnInit } from "@angular/core";
import { BaseComponent } from "../../shared/base.component";
import { ExportTemplateStyle, ExportViewType } from "../../../models/export-template.model";
import { SelectItem } from "primeng/api";
import { ExportTemplateService } from "../../../services/export-template.service";
import * as _ from "lodash";
import { MessagesObservableService } from "../../../services/messages-observable.service";
import { CompanySettingsService } from "../../../services/settings.service";

@Component({
    selector: 'd3s-admin-export-template-style-form',
    template: `
    <header>{{mode}} Styling Rules</header>
    <div class="clear"></div>
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div *ngIf="!isLoading">
        <form #styleForm="ngForm" (ngSubmit)="save()">
            <div class="row">
                <div class="col l6 m6 s12">
                    <div class="FieldName">Selection</div>
                    <div class="row">
                        <div class="col s12">
                            <p-dropdown [options]="selections" [(ngModel)]="model.SelectionType" (onChange)="selectionChange();" [ngModelOptions]="{standalone: true}" [style]="{ 'width' : '50%' }"></p-dropdown>
                        </div>
                    </div>
                </div>
                <div class="col l6 m6 s12">
                    <div class="FieldName">Background Color</div>
                    <div class="row">
                        <div class="col s12">
                            <p-colorPicker [(ngModel)]="model.BgColor"  name="backcolor"></p-colorPicker>
                            <input type="text" [(ngModel)]="model.BgColor" name="backcolortext" style="padding:2px;width:65px" />
                        </div>
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col l6 m6 s12">
                    <div class="FieldName">{{model.SelectionType}} Number</div>
                    <div class="row">
                        <div class="col s12">
                            <input *ngIf="model.SelectionType == 'Column'" min="1" max="99999"  name="displayColumn" style="height:25px;width:50%;display:block;" type="number" [(ngModel)]="model.Column" required />
                            <input  *ngIf="model.SelectionType == 'Row'"  min="1" max="99999" name="displayRow" style="height:25px;width:50%;display:block;" type="number" [(ngModel)]="model.Row" required />
                            <input  *ngIf="model.SelectionType == 'Header'" disabled min="1" max="99999" name="displayRow" style="height:25px;width:50%;display:block;" type="number" [(ngModel)]="model.Row" required />
                        </div>
                    </div>
                </div>
                <div class="col l6 m6 s12">
                    <div class="FieldName">Text Color</div>
                    <div class="row">
                        <div class="col s12">
                            <p-colorPicker [(ngModel)]="model.TextColor" name="textcolor"></p-colorPicker>
                            <input type="text" [(ngModel)]="model.TextColor" name="textcolortext" style="padding:2px;width:65px" />
                        </div>
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col l6 offset-l6 m6 offset-m6 s12">
                    <div class="FieldName">Bold Text</div>
                    <div class="row">
                        <div class="col s12">
                            <input pCheckbox type="checkbox" name="boldText" [(ngModel)]="model.IsBold" />
                        </div>
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col s12 buttons">
                    <button pButton type="submit" style="width: '150px';" label="Save" [disabled]="!styleForm.form.valid"></button>
                    <button pButton type="button" style="width: '150px';" label="Cancel" (click)="onCancel.emit(null)"></button>
                </div>
            </div>
        </form>
    </div>

`
})
export class AdminExportTemplateStyleFormComponent extends BaseComponent implements OnChanges, FormEvents  {
    @Input() mode: string;
    @Input() exportViewType: ExportViewType;
    @Input() selectedStyle: ExportTemplateStyle;
    @Output() onComplete = new EventEmitter();
    @Output() onSuccess = new EventEmitter();
    @Output() onError = new EventEmitter();
    @Output() onCancel = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Input() templateId: number;

    private model: ExportTemplateStyle ;
    selections: SelectItem[] = [{ label: "Column", value: "Column" }, { label: "Row", value: "Row" }];

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
        if (changes["mode"].currentValue == 'Edit' && changes["selectedStyle"].currentValue) {
            this.model = _.clone(changes["selectedStyle"].currentValue);
        }
        this.model.SelectionType = this.exportViewType.toString() != ExportViewType[ExportViewType.Pivot] ? "Header" : this.model.Column == -1 ? "Row" : "Column";          

        if (this.exportViewType.toString() == ExportViewType[ExportViewType.Pivot])        
            this.selections = [{ label: "Column", value: "Column" }, { label: "Row", value: "Row" }];
        else
            this.selections = [{ label: "Header", value: "Header" }];

    }
    selectionChange() {
        if (this.model.SelectionType == "Row" || this.model.SelectionType == "Header") {
            this.model.Column = -1;
            this.model.Row = 1;
        } else {
            this.model.Column = 1;
            this.model.Row = -1;
        }
    }
    save() {
        this.exportTemplateService.saveExportTemplateStyle(this.model).subscribe(result => {
            if (this.model.ID) {
                this.messagesService.showInfoMessage('Success', 'Style updated successfully');
            } else {
                this.messagesService.showInfoMessage('Success', 'Style added successfully');
            }            
            //default
            this.model = { SelectionType: "Row", BgColor: "#FFFFFF", TextColor: "#000000", Column: null, Row: null, ID: 0, AssetTypeExportTemplateID: 0, IsBold: false };
            this.onSuccess.emit(null);
        });
    }
} 