import { Input, Component, forwardRef, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange, NgModule  } from "@angular/core";
import * as _ from "lodash";
import { NG_VALUE_ACCESSOR, ControlValueAccessor, FormsModule } from "@angular/forms";
import { SortOrder } from "../../models/enums.model";
import { EditorField } from "../../models/editor-field.model";
import { BaseComponent } from "./base.component";
import { ResourcesService } from "../../services/resources.service";
import { LazyLoadEvent, SharedModule } from "primeng/api";
import { CommonModule } from "@angular/common";
import { HttpClientModule, HTTP_INTERCEPTORS } from "@angular/common/http";
import { RouterModule } from "@angular/router";
import { MultiSelectModule } from "primeng/multiselect";
import { InputSwitchModule } from "primeng/inputswitch";
import { InputTextModule } from "primeng/inputtext";
import { TreeTableModule } from "primeng/treetable";
import { ButtonModule } from "primeng/button";
import { DropdownModule } from "primeng/dropdown";
import { SelectButtonModule } from "primeng/selectbutton";
import { AutoCompleteModule } from "primeng/autocomplete";
import { TableModule } from "primeng/table";
import { EditorModule } from "primeng/editor";
import { TooltipModule } from "primeng/tooltip";
import { ToastModule } from "primeng/toast";
import { CoreModule } from "./core.module";
import { PipesModule } from "../../pipes/pipes.module";
import { SharedDynamicGridEditorModule } from "./dynamicgrideditor/shared-dynamic-grid-editor.module";
import { SharedGridPagingInfoModule } from "./grid-paging-info.component";
import { TilesModule } from "./tiles/tiles.module";
import { SiteModalModule } from "./modal/gov-modal.module";
import { SharedObjectDetailsModule } from "./objectdetails/shared-object-details.module";
import { SimpleAccordionModule } from "./simple-accordion.part";
import { CompanySettingsService } from "../../services/settings.service";
import { SearchFieldModule } from "./controls/search-field/search-field.component";


export const RESOURCE_MULTISELECT_GRID_VALUE_ACCESSOR: any = {
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => ResourceMultiSelectGridComponent),
    multi: true
};

@Component({
    selector: 'd3s-resource-multiselect-grid',
    template: `                
                <span>
                    <ig-search-field *ngIf="showSimpleFilter"
                                     mode="Keypress"
                                     [debounce]="600"
                                     (onSearch)="dt.filterGlobal($event, 'contains')"
                                     [(ngModel)]="simpleTextFilter"
                                     [infoTooltip]="simpleSearchTooltipHTML">
                    </ig-search-field>
                    <p-table #dt [value]="items" [selectionMode]="multiple ? 'multiple' : 'single'" [scrollable]="true" scrollWidth="100%" [lazy]="true" [totalRecords]="totalRecords" [metaKeySelection]="!multiple" 
                        [globalFilterFields]="['Text','Type']" [pageLinks]="3" [paginator]="true" [rows]="rowsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [loading]="isLoading" 
                        loadingIcon="fa fa-spinner" [selection]="selectedItems" (selectionChange)="handleItemSelection($event);"  (onLazyLoad)="lazyLoad($event)">
                        <ng-template pTemplate="colgroup">
                            <colgroup>
                                <col style="width:38px">
                                <col >
                                <col >
                                <col style="width:5%" *ngIf="showToolTip">
                            </colgroup>
                        </ng-template>
                        <ng-template pTemplate="header">
                            <tr>
                                <th style="width: 38px"><p-tableHeaderCheckbox *ngIf="multiple"></p-tableHeaderCheckbox></th>
                                <th [pSortableColumn]="'Text'">
                                    Name
                                    <d3s-sortIcon [field]="'Text'"></d3s-sortIcon>
                                </th>
                                <th [pSortableColumn]="'Type'" *ngIf="showResourceType">
                                    Resource Type
                                    <d3s-sortIcon [field]="'Type'"></d3s-sortIcon>
                                </th>
                                <th style="width: 5%" *ngIf="showToolTip"></th>
                            </tr>
                        </ng-template>
                        <ng-template pTemplate="body" let-item>
                            <tr [pSelectableRow]="item">
                                <td>
                                    <p-tableCheckbox *ngIf="multiple" [value]="item"></p-tableCheckbox>
                                    <p-tableRadioButton *ngIf="!multiple" [value]="item"></p-tableRadioButton>
                                </td>
                                <td>{{item.Text}}</td>
                                <td>{{item.Type}}</td>
                                <td *ngIf="showToolTip">
                                    <div class="RowTools">
                                        <d3s-preview-tooltip [objectType]="item.Value.split('|')[0]" [objectId]="item.Value.split('|')[1]" icon="info"></d3s-preview-tooltip>
                                    </div>
                                </td>
                            </tr>
                        </ng-template>
                        <ng-template *ngIf="true" pTemplate="summary">
                            <d3s-grid-paging-info [first]="dt.first" [rows]="dt.rows" [totalRecords]="dt.totalRecords"></d3s-grid-paging-info>
                            <div *ngIf="showSelectedSummary && selectedItems && selectedItems.length > 0" class="multiselect-grid-sel">Selected Items:
                                <p *ngIf="selectedItems && selectedItems.length > 0"><span *ngFor="let item of selectedItems;let last = last" >{{last?item.Text:item.Text +','}} </span></p>
                            </div>
                        </ng-template>
                    </p-table>
                </span>
                `,
    providers: [RESOURCE_MULTISELECT_GRID_VALUE_ACCESSOR],
    changeDetection: ChangeDetectionStrategy.OnPush, 
})

export class ResourceMultiSelectGridComponent extends BaseComponent implements OnChanges,ControlValueAccessor  {   
    
   @Input("field") field: EditorField;
    @Input() multiple: boolean = true;
    @Input() showToolTip: boolean = true;
    @Input() showSelectedSummary: boolean = true;
    @Input() showResourceType: boolean= false;
    value: any; //stores the values array bound back to the ngform.

    totalRecords: number;
    rowsPerPage: number = 10;
    currentPageNumber: number = 0;
    sortField: string;
    sortOrder: SortOrder = SortOrder.None;
    globalfilter: string;

    items: any[];
    selectedItems: any;

    public onModelChange: Function = () => { };

    public onModelTouched: Function = () => { };

    constructor(
        private resourceService: ResourcesService,
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange} ) {
        if ((changes["field"].previousValue != null) &&
            (changes["field"].currentValue.TypeaheadUri != changes["field"].previousValue.TypeaheadUri)) {
            this.load();
        }
    }
    private load() {
        this.isLoading = true;
       
        this.sortField = this.sortField == null ? "" : this.sortField;
        this.globalfilter = this.globalfilter == null ? "" : this.globalfilter;
        
        let url = `${this.field.TypeaheadUri}&pagenum=${this.currentPageNumber}&pagesize=${this.rowsPerPage}&sortdatafield=${this.sortField}&sortorder=${this.sortOrder == SortOrder.None ? "" : (this.sortOrder == SortOrder.Ascending ? "asc" : "desc")}&gbfilter=${this.globalfilter}`;
        
        this.resourceService.getResourceItems(url).
            subscribe((data) => {
                this.isLoading = false;
                this.items = data.results;
                this.totalRecords = data.total;
                this.ref.markForCheck();
            });
    }

    lazyLoad(event: LazyLoadEvent) {
        this.sortOrder = event.sortOrder;
        this.sortField = event.sortField;
        this.rowsPerPage = event.rows;
        this.currentPageNumber = event.first / event.rows;
        this.globalfilter = event.globalFilter;
        this.load();
    }

    handleItemSelection(event) {
        if (this.multiple) {
            this.selectedItems = event;
            let seletions = [];
            for (let item of event) {
                seletions.push(item.Value);
            }
            this.value = _.cloneDeep(seletions);
            this.onModelChange(this.value);
        }
        else {
            var selections = [];
            selections.push(event.Value);
            var sel = [];
            sel.push(event);
            this.selectedItems = sel;
            this.value = _.cloneDeep(selections);
            this.onModelChange(this.value);
        }
    }

    handleKeyPress() {
        this.ref.markForCheck();
    }

    writeValue(value: any): void {
        this.value = value;
    }

    registerOnChange(fn: Function): void {
        this.onModelChange = fn;
    }

    registerOnTouched(fn: Function): void {
        this.onModelTouched = fn;
    }
}

@NgModule({
    declarations: [
        ResourceMultiSelectGridComponent
    ],
    exports: [
        ResourceMultiSelectGridComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //primeng
        ToastModule,
        InputSwitchModule,
        InputTextModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        SelectButtonModule,
        AutoCompleteModule,
        MultiSelectModule,
        EditorModule,
        TooltipModule,
        SharedModule,
        TableModule,
        SearchFieldModule,
        //d3s
        CoreModule,
        PipesModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SimpleAccordionModule,
        TilesModule,
        SiteModalModule,
    ],
    providers: []
})

export class ResourceMultiSelectGridModule { }