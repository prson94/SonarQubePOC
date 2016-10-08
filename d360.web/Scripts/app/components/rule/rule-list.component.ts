import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RulesService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { RuleDimension, Rule, RuleClassification } from '../../models/rule.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';


@Component({
    selector: 'd3s-rule-list',
    providers: [RulesService],
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <d3s-loading [isLoading]="isLoading"></d3s-loading>
                        <div class="tile tile-detail" >    
                            <div class="row" *ngIf="!isLoading && !showDelete && !showEditor">                        
                                <div class="col s12">
                                    <header>{{modelGroup}} Rules                                
                                        <d3s-tile-actions [hasAdd]="true" (addClick)="showAddRule()"></d3s-tile-actions>                                                     
                                    </header>      
                                    <input #gb type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">                                                                                     
                                    <p-dataTable [globalFilter]="gb" [value]="rules" selectionMode="single" [rows]="20" [rowsPerPageOptions]="[5,10,20]" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;showRule();" >
                                        <p-column field="ID" header="ID" [sortable]="true" [style]="{width:'10%'}"></p-column>                                                                                                                        
                                        <p-column field="Name" header="Name" [sortable]="true" [style]="{width:'45%'}"></p-column>                                                                                                                        
                                        <p-column field="RuleType" header="Type" [sortable]="true" [style]="{width:'15%'}">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span>{{getRuleTypeText(data.RuleType)}}</span>
                                            </template>                          
                                        </p-column>
                                        <p-column field="Dimension" header="Dimension" [sortable]="true" [style]="{width:'15%'}">
                                            <template let-col let-data="rowData" pTemplate type="body">
                                                <span>{{data.Dimension?.Name}}</span>
                                            </template>                          
                                        </p-column>
                                        <p-column [style]="{width:'40px'}">
                                            <template let-item="rowData" pTemplate type="body">
                                                <div class="RowTools">
                                                    <a style="cursor:pointer;" (click)="selected=item;showEditor=true;"><i class="fa fa-pencil"></i></a>                                        
                                                </div>
                                            </template>
                                        </p-column>                            
                                        <p-column  [style]="{width:'40px'}">
                                                <template let-item="rowData" pTemplate type="body">
                                                    <div class="RowTools">                                
                                                        <a style="cursor:pointer;" (click)="selected=item;showDelete=true;"><i class="fa fa-trash-o"></i></a>                                    
                                                    </div>
                                                </template>
                                        </p-column> 
                                    </p-dataTable>      
                                </div>
                            </div>
                            <d3s-dynamic-editor *ngIf="showEditor" [objectID]="selected?.ID" [objectType]="'Rule'" [title]="'Rule'" [selection]="selected" (saveClick)="saveRule($event)" (closeClick)="showEditor = false;"></d3s-dynamic-editor>
                            <delete-form *ngIf="showDelete"
                                                    [callback]="theDeleteCallback"
                                                    [itemId]="selected?.ID"
                                                    [method]="'callback'"
                                                    [prompt]="'Are you sure you want to delete the selected item?'"                                         
                                                    (onCancel)="showDelete=false;"
                            ></delete-form>  
                        </div>                        
                    </div>
                </div>
                `
})

export class RuleListComponent extends BaseComponent implements OnInit {
    private rules: Rule[] = [];
    private selected: Rule;
    private showEditor: boolean = false;
    private showDelete: boolean = false;

    theDeleteCallback: Function;
    
    constructor(private route: ActivatedRoute,
        private router: Router,
        protected rulesService: RulesService, protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();

        this.theDeleteCallback = this.deleteRule.bind(this);
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Rules');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Rules'));

        this.loadRules();
    }

    private loadRules() {
        this.isLoading = true;
        this.rulesService.getRules()
            .then(result => {
                this.isLoading = false;
                this.rules = result;                
                if (this.rules.length && this.rules.length > 0) this.selected = this.rules[0];
            });
    }

    private showAddRule() {
        this.selected = null;
        this.showEditor = true;
    }

    private saveRule(event) {
        this.rulesService.saveRule(event.item)
            .then(result => {
                if (event.item.ID == undefined) {
                    event.item.ID = Number(result.id);
                    this.rules[this.rules.length] = event.item;
                }
                else {
                    this.rules[this.findRuleIndex(event.item.ID)] = event.item;
                }
                this.selected = event.item;
                this.showEditor = false;
            });
    }

    private showRule() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_RULE_ROOT}/${this.selected.ID}`)
    }

    findRuleIndex(id: number) {
        var index: number = -1;
        for (var rule of this.rules) {
            index++;
            if (rule.ID == id) return index;
        }
    }


    private deleteRule(id: number) {
        this.rulesService.deleteRule(id);
        this.showDelete = false;
        this.selected = this.rules.length > 0 ? this.rules[0] : null;
        this.rules.splice(this.findRuleIndex(id), 1);
    }

    private getRuleTypeText(ruleType: RuleClassification): string {
        return RuleClassification[ruleType];
    }
};