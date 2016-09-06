///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, OnInit, Input, EventEmitter, Output} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { SearchService } from '../../services/index';
import { SearchResultsObject, SearchResultInfo, SearchCategories } from '../../models/search-result.model';

@Component({
    selector: 'd3s-search-results',
    template: `               
                <div *ngIf="results?.Result?.Results?.length > 0">
                    <div class="row">
                        <div class="col l2 m4 hide-on-small-only">
                            <div>
                                <h4 class="search-result-categories">Categories</h4>
                                <div class="widget search-category-area" id="CategoryResults">
                                    <div class="row">
                                        <div class="col l10 m10 s11 entry">                                            
                                            <a (click)="clearCategoryFilter()" style="cursor:pointer" class="search-type-link" [title]="'All'" [ngClass]="{selected:!selectedCategory}">All</a>                                            
                                        </div>
                                        <div class="col l2 m2 s1">
                                            <span style="float:right">{{results?.Result?.Matches}}</span>
                                        </div>                                        
                                    </div>
                                    <template let-category ngFor [ngForOf]="categories">
                                        <div class="row">
                                            <div class="col l10 m10 s11 entry">
                                                <i class="search-category-type-group fa fa-angle-right" data-bind="click: toggleVisibility,visible: showToggle,css: {'fa-angle-right' : showRow, 'fa-angle-down' : !showRow()}"></i>
                                                <a (click)="selectCategory(category);" style="cursor:pointer" class="search-type-link" [title]="category.DisplayName" [ngClass]="{selected:category.DisplayName==selectedCategory?.DisplayName}">{{category.DisplayName}}</a>                                            
                                            </div>
                                            <div class="col l2 m2 s1">
                                                <span style="float:right">{{category?.ResultCount}}</span>
                                            </div>                                        
                                        </div>
                                        <div class="row" *ngFor="let subCategory of category?.Categories">
                                            <div class="col l10 m10 s11 entry">                                            
                                                <a (click)="selectCategory(subCategory);" style="cursor:pointer" [ngClass]="{selected:subCategory.Name==selectedCategory?.Name}" class="search-category-link" [title]="subCategory.Name">{{subCategory.Name}}</a>
                                            </div>
                                            <div class="col l2 m2 s1">
                                                <span style="float:right">{{subCategory?.ResultCount}}</span>
                                            </div>                                        
                                        </div>
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="col l10 m8">
                            <div>                                
                                <h6 class="search-result-summary">Search found {{ results?.Result?.Matches }} matches in ({{results?.Result?.ElapsedMS /1000}} seconds)</h6>
                                <div *ngFor="let result of results?.Result?.Results">
                                    <d3s-search-result-item [result]="result"></d3s-search-result-item>
                                </div>
                                <p-paginator [rows]="itemsPerPage" [totalRecords]="results?.Result?.Matches" (onPageChange)="paginate($event)"></p-paginator>
                            </div>
                        </div>
                    </div>
                </div>

                `,
    providers: [SearchService],
})

export class SearchResultsComponent extends BaseComponent implements OnInit {
    @Input() results: SearchResultsObject;
    @Input() categories: SearchCategories[] = [];    
    @Input() itemsPerPage: number = 5;

    
    @Output() categoryClick = new EventEmitter();
    @Output() paginateClick = new EventEmitter();

    private selectedCategory = SearchCategories;

    constructor() {
        super();
    }

    ngOnInit() {

    }

    private clearCategoryFilter() {
        this.selectedCategory = null;
        this.categoryClick.emit({ category: null});
    }

    private selectCategory(category) {
        this.selectedCategory = category;
        this.categoryClick.emit({ category: this.selectedCategory });
    }

    private paginate(data) {
        /*
            event.page: New page number
            event.first: Index of first record
            event.rows: Number of rows to display in new page            
            event.pageCount: Total number of pages
        */        
        this.paginateClick.emit({page: data.page, size: data.rows, first: data.first});
    }
};