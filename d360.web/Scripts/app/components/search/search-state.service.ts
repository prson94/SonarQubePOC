import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SearchFullResult, AdvancedSearchFilter, SearchQuery, SearchAggregationFilter, SearchFieldFilter, SearchState } from '../../models/search-result.model';
import { debounceTime } from 'rxjs/operators';
import { Observable, BehaviorSubject } from 'rxjs';
import { BaseObservableService } from '../../services/baseObservable.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';
import { SearchService } from '../../services/search.service';
import { SettingsHelper } from '../../models/settings.model';

@Injectable()
export class SearchStateService extends BaseObservableService {

    private readonly sessionKey:string = 'd3sSearchState';
    private readonly sessionAgeMinutes: number = 10;
    private searchService: SearchService;

    constructor(private http: HttpClient, messagesService: MessagesObservableService) {
        super(messagesService);
        this.searchService = new SearchService(http, messagesService);
        this.reset();
    }

    private _categories: BehaviorSubject<CheckTreeNode[]> = new BehaviorSubject([]);
    get currentCategories() {
        return this._categories.value;
    }
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
    private _pageNumber: BehaviorSubject<number> = new BehaviorSubject(0);
    get pageNumber() {
        return new Observable(fn => this._pageNumber.subscribe(fn));
    }
    private _loading: BehaviorSubject<boolean> = new BehaviorSubject(false);
    get loading() {
        return new Observable(fn => this._loading.subscribe(fn));
    }
    private _treeLoading: BehaviorSubject<boolean> = new BehaviorSubject(false);
    get treeLoading() {
        return new Observable(fn => this._treeLoading.subscribe(fn));
    }

    public selectedFilters: CheckTreeNode[];
    public advancedFilters: AdvancedSearchFilter[]; 

    private _checkTreeKeys: string[];
    private _query: SearchQuery;
    private _aggFilters: SearchAggregationFilter[];
    private _searchTypes: string[];
    private _needAggregation: boolean = false;

    loadState(term: string, searchCategotries: string[]) {
        this._resultCount.next(0);
        this._results.next([]);
        this._categories.next([]);
        this._query = new SearchQuery({
            Term: "",
            From: 0,
            Size: 10,
            AggregationFilters: [],
            FieldFilters: [],
            Aggregations: []
        });

        let sess: SearchState[] = JSON.parse(sessionStorage.getItem(this.sessionKey));
        let limit = new Date().getTime() - (this.sessionAgeMinutes * 60000)
        if (sess != null && sess.findIndex(q => q.Term == term && new Date(q.Querytime).getTime() > limit) >= 0) {
            let state = sess.find(q => q.Term == term);
            this._query = state.Query;
            this._aggFilters = state.AggFilters;
            this._searchTypes = state.SearchTypes;
            this.advancedFilters = state.AdvancedFilters;
            this._checkTreeKeys = state.CheckTreeKeys;
        }

        this.setSearchCategories(searchCategotries);
    }

    private saveState() {
        let sess: SearchState[] = JSON.parse(sessionStorage.getItem(this.sessionKey));
        if (sess == null) {
            sess = [];
        } else {
            let limit = new Date().getTime() - (this.sessionAgeMinutes * 60000)
            sess = sess.filter(q => q.Term != this._query.Term && new Date(q.Querytime).getTime() > limit);
        }
        let state = new SearchState({
            Term: this._query.Term,
            Query: this._query,
            AggFilters: this._aggFilters,
            SearchTypes: this._searchTypes,
            CheckTreeKeys: (this._checkTreeKeys !== []) ? this._checkTreeKeys : this.selectedFilters.map(f => f.key),
            AdvancedFilters: this.advancedFilters,
            Querytime: new Date()
        });
        sess.push(state);
        sessionStorage.setItem(this.sessionKey, JSON.stringify(sess));
    }

    /**
     * Resets search state
     */
    reset() {
        this._resultCount.next(0);
        this._results.next([]);
        this._categories.next([]);
        this._aggFilters = [];
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


    /**
     * Sets search categories that search will be limited to. These will be combined with aggregation filters
     * Only set search categories if no select filters are set
     * @param searchCategories
     */
    setSearchCategories(searchCategories: string[]) {
        if (this.selectedFilters == undefined || this.selectedFilters.length == 0) {
            this._searchTypes = searchCategories.sort().filter((x, i, a) => !i || x != a[i - 1]);
            this.selectedFilters = [];
        }
    }

    /**
     * Set/replace aggreagtion filter values
     * @param field
     * @param values
     * @param replace
     */
    setAggregationFilter(field: string, values: string[], replace: boolean = true) {
        var idx = this._aggFilters.findIndex((f) => f.Field === field);
        if (idx === -1) {
            this._aggFilters.push(new SearchAggregationFilter({
                Field: field,
                Values: values.sort().filter((x, i, a) => !i || x != a[i - 1])
            }));
            this._query.From = 0;
        } else if (replace) {
            this._aggFilters[idx].Values = values.sort().filter((x, i, a) => !i || x != a[i - 1]);
            this._query.From = 0;
        } else {
            let precount = this._aggFilters[idx].Values.length;
            this._aggFilters[idx].Values = this._aggFilters[idx].Values.concat(values).sort().filter((x, i, a) => !i || x != a[i - 1]);
            let postcount = this._aggFilters[idx].Values.length;
            if (precount != postcount)
                this._query.From = 0;
        }
    }

    private combineAggFilters(one: SearchAggregationFilter[], two: SearchAggregationFilter[]): SearchAggregationFilter[] {
        let retVal = one.slice();
        two.forEach(function (item) {
            let idx = retVal.findIndex((f) => f.Field === item.Field);
            if (idx === -1) {
                retVal.push(item);
            } else {
                retVal[idx].Values = retVal[idx].Values.concat(item.Values).sort().filter((x, i, a) => !i || x != a[i - 1]);
            }
        });
        return retVal;
    }

    /**
     * Set Advanced filters on search
     * @param advFilters
     * @param replace
     */
    setFieldFilters(advFilters: AdvancedSearchFilter[], replace: boolean = true) {
        if (advFilters.length != this._query.FieldFilters.length) {
            this._query.FieldFilters = [];
            this._needAggregation = true;
        }
        advFilters.forEach(function (item) {
            var field = item.field == "Tags" ? "d3sTags" : item.field;
            var idx = this._query.FieldFilters.findIndex((f) => f.Field === field);
            if (idx === -1) {
                this._query.FieldFilters.push(new SearchFieldFilter({
                    Field: field,
                    Phrase: item.value,
                    MatchWords: item.exact
                }));
                this._needAggregation = true;
            } else if (replace) {
                if (this._query.FieldFilters[idx].Phrase != item.value) {
                    this._query.FieldFilters[idx].Phrase = item.value;
                    this._needAggregation = true;
                }
                if (this._query.FieldFilters[idx].MatchWords != item.exact) {
                    this._query.FieldFilters[idx].MatchWords = item.exact;
                    this._needAggregation = true;
                }               
            }
        }, this);
    }

    /**
     * Sets explain flag on query
     * @param explain
     */
    setExplain(explain: boolean) {
        this._query.Explain = explain;
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
        this.saveState();
        this._loading.next(true);

        //If searchTypes are set, retrieve and apply, then set to empty as we'll rely on selectedFilters going forward
        let searchTypes = this._searchTypes.sort().filter((x, i, a) => !i || x != a[i - 1]);
        this._searchTypes = [];

        if (this._needAggregation || this._categories.value.length == 0) {
            this._treeLoading.next(true);
            this._categories.next([]);

            //New aggregation, so this should be a new search, jump to first page
            if (this._needAggregation)
                this._query.From = 0;

            var aggQuery = Object.assign({}, this._query);
            aggQuery.Aggregations = ['category'];
            aggQuery.Size = 0;
            aggQuery.AggregationFilters = [];

            this.searchService.getSearchResultsByQuery(aggQuery).pipe(
                debounceTime(1000)
            ).subscribe(res => {
                if (res.Categories.length != 0) {
                    var filterTree = res.Categories.map((val) => {
                        return {
                            "key": val.Name,
                            "label": this.getDisplayLookup(val.Name),
                            "type": "category",
                            "expanded": false,
                            "data": val.Name,
                            "count": val.ResultCount,
                            "children": val.Categories.map((cat) => {
                                return {
                                    "key": val.Name + '___' + cat.Name,
                                    "label": cat.Name,
                                    "type": "subCategory",
                                    "data": cat.Name,
                                    "count": cat.ResultCount
                                };
                            })
                        }
                    });
                    let selectedFilters = [];
                    if (this._checkTreeKeys != undefined && this._checkTreeKeys.length > 0) {
                        for (let key of this._checkTreeKeys) {
                            let node = this.getNodeWithKey(key, filterTree);
                            if (node) {
                                selectedFilters.push(node);
                            }
                        }
                        this.selectedFilters = selectedFilters;
                        this._checkTreeKeys = [];
                    } else if (searchTypes.length > 0) {
                        for (let key in searchTypes) {
                            let node = this.getNodeWithKey(searchTypes[key], filterTree);
                            if (node) {
                                selectedFilters.push(node);
                            }
                        }
                        this.selectedFilters = selectedFilters;
                    }
                    this._categories.next(filterTree);
                    this._needAggregation = false;
                }
                this._treeLoading.next(false);
            });
        }

        this._query.AggregationFilters = this.combineAggFilters(this._aggFilters, [new SearchAggregationFilter({
            Field: "d3sCategory",
            Values: searchTypes
        })]);

        this.searchService.getSearchResultsByQuery(this._query).pipe(
            debounceTime(1000)).subscribe(res => {
            this._resultCount.next(res.Result.Matches);
            this._pageNumber.next(this._query.From / this._query.Size);
            this._results.next(res.Result.Results);
            this._loading.next(false);
        });
    }

    getNodeWithKey(key: string, nodes: CheckTreeNode[]) {
        for (let node of nodes) {
            if (node.key === key) {
                return node;
            }

            if (node.children) {
                let matchedNode = this.getNodeWithKey(key, node.children);
                if (matchedNode) {
                    return matchedNode;
                }
            }
        }
    }
}