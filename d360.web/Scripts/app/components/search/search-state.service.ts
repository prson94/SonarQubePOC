import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SearchFullResult, AdvancedSearchFilter, SearchQuery, SearchAggregationFilter, SearchFieldFilter } from '../../models/search-result.model';
import { debounceTime } from 'rxjs/operators';
import { Observable, BehaviorSubject } from 'rxjs';
import { BaseObservableService } from '../../services/baseObservable.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';
import { SearchService } from '../../services/search.service';
import { SettingsHelper } from '../../models/settings.model';

@Injectable()
export class SearchStateService extends BaseObservableService {

    private searchService: SearchService;

    constructor(private http: HttpClient, messagesService: MessagesObservableService) {
        super(messagesService);
        this.searchService = new SearchService(http, messagesService);
        this.reset();
    }

    private _categories: BehaviorSubject<CheckTreeNode[]> = new BehaviorSubject([]);
    
    get categories() {
        return new Observable(fn => this._categories.subscribe(fn));
    }
    private _results: BehaviorSubject<SearchFullResult[]> = new BehaviorSubject([]);
    get results() {
        return new Observable(fn => this._results.subscribe(fn));
    }
    private _resultCount: BehaviorSubject<number> = new BehaviorSubject(0);
    get resultCount() {
        return new Observable(fn => this._resultCount.subscribe(fn));
    }
    private _loading: BehaviorSubject<boolean> = new BehaviorSubject(false);
    get loading() {
        return new Observable(fn => this._loading.subscribe(fn));
    }

    private _query: SearchQuery;
    private _searchTypes: string[];
    private _needAggregation: boolean = false;

    /**
     * Resets search state
     */
    reset() {
        this._resultCount.next(0);
        this._results.next([]);
        this._categories.next([]);
        this._searchTypes = [];
        this._query = new SearchQuery({
            Term: "",
            From: 0,
            Size: 10,
            AggregationFilters: [],
            FieldFilters: [],
            Aggregations: []
        });
    }

    private _displayNameLookup: string[];
    private getDisplayLookup(category: string) {
        if (this._displayNameLookup == undefined) {
            this._displayNameLookup = SettingsHelper.getSearchTypesList().reduce(function (map, obj) {
                map[obj.value] = obj.title;
                return map;
            }, []);
        }
        if (this._displayNameLookup[category] != undefined)
            return this._displayNameLookup[category];
        else
            return category;
    }

    private areDifferent(a: string[], b: string[]): boolean {
        if (a.length !== b.length) {
            return true;
        }
        for (var i = 0; i < a.length; ++i) {
            if (a[i] !== b[i]) {
                return true;
            }
        }
        return false;
    }

    /**
     * Sets search categories that search will be limited to. These will be combined with aggregation filters
     * If search categories change, aggregation filters will be reset
     * @param searchCategories
     */
    setSearchCategories(searchCategories: string[]) {
        var cat = searchCategories.sort().filter((x, i, a) => !i || x != a[i - 1]);
        if (this.areDifferent(cat, this._searchTypes)) {
            this._searchTypes = cat;
            this._query.AggregationFilters = [];
            this._needAggregation = true;
        }
    }

    /**
     * Set/replace aggreagtion filter values
     * @param field
     * @param values
     * @param replace
     */
    setAggregationFilter(field: string, values: string[], replace: boolean = true) {
        var idx = this._query.AggregationFilters.findIndex((f) => f.Field === field);
        if (idx === -1) {
            this._query.AggregationFilters.push(new SearchAggregationFilter({
                Field: field,
                Values: values.sort().filter((x, i, a) => !i || x != a[i - 1])
            }));
        } else if (replace) {
            this._query.AggregationFilters[idx].Values = values.sort().filter((x, i, a) => !i || x != a[i - 1]);
        } else {
            this._query.AggregationFilters[idx].Values = this._query.AggregationFilters[idx].Values.concat(values).sort().filter((x, i, a) => !i || x != a[i - 1]);
        }
    }

    /**
     * Set Advanced filters on search
     * @param advFilters
     * @param replace
     */
    setFieldFilters(advFilters: AdvancedSearchFilter[], replace: boolean = true) {
        if (replace)
            this._query.FieldFilters = [];
        advFilters.forEach(function (item) {
            var field = item.field == "Tags" ? "d3sTags" : item.field;
            var idx = this._query.FieldFilters.findIndex((f) => f.Field === field);
            if (idx === -1) {
                this._query.FieldFilters.push(new SearchFieldFilter({
                    Field: field,
                    Phrase: item.value,
                    MatchWords: item.exact
                }));
            } else {
                this._query.FieldFilters[idx].Phrase = item.value;
                this._query.FieldFilters[idx].MatchWords = item.exact;
            }
        }, this);
        this._needAggregation = true;
    }

    /**
     * Perform search of <term>
     * @param term
     */
    search(term: string) {
        if (term != this._query.Term) {
            this._query.Term = term;
            this._query.From = 0;
            this._needAggregation = true;
        }
        this.doSearch();
    }

    /**
     * Paginate search results
     * @param from
     * @param size
     */
    page(from: number, size: number) {
        this._query.From = from;
        this._query.Size = size;
        this.doSearch();
    }

    /**
     * Performs search and updates observable values
     */
    private doSearch() {
        this._loading.next(true);
        this._query.Aggregations = (this._needAggregation || this._categories.value.length == 0) ? ['category'] : [];
        if (this._query.Aggregations.length > 0) {
            this._categories.next([]);
        }
        this.setAggregationFilter('d3sGroup', this._searchTypes, false);

        this.searchService.getSearchResultsByQuery(this._query).pipe(
            debounceTime(1000)).subscribe(res => {
            if (res.Categories.length != 0) {
                var filterTree = res.Categories.map((val) => {
                    return {
                        "label": this.getDisplayLookup(val.Name),
                        "type": "category",
                        "expanded": true,
                        "data": val.Name,
                        "count": val.ResultCount,
                        "children": val.Categories.map((cat) => {
                            return {
                                "label": cat.Name,
                                "type": "subCategory",
                                "data": cat.Name,
                                "count": cat.ResultCount
                            };
                        })
                    }
                });
                this._categories.next(filterTree);
                this._needAggregation = false;
            }
            this._resultCount.next(res.Result.Matches);
            this._results.next(res.Result.Results);
            this._loading.next(false);
        });
    }
}