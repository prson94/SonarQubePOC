import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SearchFullResult, AdvancedSearchFilter, SearchQuery, SearchAggregationFilter, SearchFieldFilter, SearchState, SearchCheckTreeVal } from '../../models/search-result.model';
import { tap, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Observable, BehaviorSubject, pipe, Subscription } from 'rxjs';
import { BaseObservableService } from '../../services/baseObservable.service';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { AuthenticationService } from '../../services/authentication.service';
import { CheckTreeNode } from '../shared/small-widgets/check-tree/checktreenode';
import { SearchService } from '../../services/search.service';
import { SearchType } from '../../models/settings.model';
import { SearchSession } from './search-session';

@Injectable()
export class SearchStateService extends BaseObservableService {

    private readonly sessionKey:string = 'd360SearchState';
    private readonly sessionAgeMinutes: number = 10;
    private readonly debounceValue: number = 400;

    private AggSub$: Subscription;
    private MainSub$: Subscription;
    private AggQuery$: BehaviorSubject<SearchQuery> = new BehaviorSubject<SearchQuery>(new SearchQuery());
    private MainQuery$: BehaviorSubject<SearchQuery> = new BehaviorSubject<SearchQuery>(new SearchQuery());

    private searchTypes: SearchType[] = []

    constructor(private http: HttpClient, messagesService: MessagesObservableService, protected authenticationService: AuthenticationService, protected searchService: SearchService) {
        super(messagesService);
        this.createQuerySubscriptions();
        this.searchService.getSearchCategories(this.authenticationService.isAdmin).subscribe((res) => this.searchTypes = res);
    }

    //Subject definitions
    private _categories: BehaviorSubject<CheckTreeNode[]> = new BehaviorSubject([]);
    get currentCategories() {
        return this._categories.value;
    }
    get categories() {
        return new Observable((fn) => this._categories.subscribe(fn));
    }
    private _results: BehaviorSubject<SearchFullResult[]> = new BehaviorSubject([]);
    get results() {
        return new Observable((fn) => this._results.subscribe(fn));
    }
    private _resultCount: BehaviorSubject<number> = new BehaviorSubject(0);
    get resultCount() {
        return new Observable((fn) => this._resultCount.subscribe(fn));
    }
    private _pageNumber: BehaviorSubject<number> = new BehaviorSubject(0);
    get pageNumber() {
        return new Observable((fn) => this._pageNumber.subscribe(fn));
    }
    private _loading: BehaviorSubject<boolean> = new BehaviorSubject(false);
    get loading() {
        return new Observable((fn) => this._loading.subscribe(fn));
    }
    private _treeLoading: BehaviorSubject<boolean> = new BehaviorSubject(false);
    get treeLoading() {
        return new Observable((fn) => this._treeLoading.subscribe(fn));
    }

    public selectedFilters: CheckTreeNode[] = [];
    public advancedFilters: AdvancedSearchFilter[]; 

    private _checkTreeKeys: SearchCheckTreeVal[] = null;
    private _query: SearchQuery;
    private _searchTypes: string[];
    private _initial: boolean = false;

    loadState(term: string, searchCategories: string[], keepFilters: boolean) {
        this.reset(keepFilters);
        this._searchTypes = searchCategories.sort().filter((x, i, a) => !i || x != a[i - 1]);

        let state = SearchSession.getState(term);
        if (state !== undefined) {
            this._query.Term = state.Term;
            this._query.From = state.From;
            this._query.Size = state.Size;
            this._searchTypes = state.SearchTypes;
            this._checkTreeKeys = state.CheckTreeKeys;
            this.advancedFilters = state.AdvancedFilters;
        }
    }

    private saveState() {
        let state = new SearchState({
            Term: this._query.Term,
            From: this._query.From,
            Size: this._query.Size,
            SearchTypes: this._searchTypes,
            CheckTreeKeys: (this._initial || this.selectedFilters == undefined) ? this._checkTreeKeys : this.selectedFilters.map(f => new SearchCheckTreeVal(f.key, f.type)),
            AdvancedFilters: this.advancedFilters,
            Querytime: new Date()
        });
        this._checkTreeKeys = (state.CheckTreeKeys == null) ? state.SearchTypes.map(k => new SearchCheckTreeVal(k, "category")) : state.CheckTreeKeys;
        SearchSession.putState(state);
    }

    /**
     * Resets search state
     */
    reset(keepFilters: boolean = false) {
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
        this._initial = !keepFilters;
        if (!keepFilters) {
            this._checkTreeKeys = null;
            this.selectedFilters = [];
            this.advancedFilters = [];
            this._pageNumber.next(0);
        }
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
     * @param resetPage
     */
    search(term: string, resetPage: boolean = false) {
        if (term != this._query.Term) {
            this._query.Term = term.substring(0,255);
            this._query.From = 0;
        }
        if (resetPage)
            this._query.From = 0;

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

        //Create the fieldFilters from the Advanced Search filter chips
        let fieldFilters = this.advancedFilters.map(item => new SearchFieldFilter({
            Field: item.field == "Tags" ? "d3sTags" : item.field,
            Phrase: item.value,
            MatchWords: item.exact
        }));

        //Create the Aggregate filters from either the checkbox tree or the provided searchTypes
        let aggFilters: SearchAggregationFilter[] = [];
        let types = [];
        let categories = [];

        if (this._initial) {
            if (this._checkTreeKeys == null) {
                this._checkTreeKeys = this._searchTypes.map(k => new SearchCheckTreeVal(k, "category"));
            }
            this._initial = false;
            types = this._checkTreeKeys.filter((x) => x.type == "subCategory").map((x) => x.key);
            categories = this._checkTreeKeys.filter((x) => x.type == "category").map((x) => x.key);
        } else {
            //Get selected Classes and AssetTypes from checkbox tree
            types = this.selectedFilters.filter((x) => x.type == "subCategory").map((x) => x.data);
            categories = this.selectedFilters.filter((x) => x.type == "category").map((x) => x.data);

            if (types.length > 0) {
                //Semi-marked classes are not "selected", so they must be added separately
                categories = categories.concat(this.currentCategories.filter((x) => x.type == "category" && x.partialSelected == true).map((x) => x.data));
                aggFilters.push(new SearchAggregationFilter({
                    Field: "d3sAssetType",
                    Values: types.sort().filter((x, i, a) => !i || x != a[i - 1])
                }));
            }
        }

        if (categories.length > 0) {
            aggFilters.push(new SearchAggregationFilter({
                Field: "d3sCategory",
                Values: categories.sort().filter((x, i, a) => !i || x != a[i - 1])
            }));
        }

        //If there are no search categories, force compareQueries to retrun false
        let force = this._categories.value.length == 0;

        this.AggQuery$.next(new SearchQuery({
            Term: this._query.Term,
            From: 0,
            Size: 0,
            AggregationFilters: [],
            FieldFilters: fieldFilters,
            Aggregations: ['category'],
            Force: force
        }));

        this.MainQuery$.next(new SearchQuery({
            Term: this._query.Term,
            From: this._query.From,
            Size: this._query.Size,
            AggregationFilters: aggFilters,
            FieldFilters: fieldFilters,
            Aggregations: [],
            Explain: this._query.Explain,
            Force: force
        }));
    }

    /**
     * Create subscriptions for the Aggregation query and Main query
     * Use distinctUntilChanged to control if a new API call is needed
     * Use Tap to set loading status
     * Use SwitchMap to ensure only one active query of the type
     */
    createQuerySubscriptions() {
        //Aggregation query - results create the checkbox tree
        this.AggSub$ = this.AggQuery$.pipe(
            debounceTime(this.debounceValue),
            distinctUntilChanged(this.compareQueries),
            tap(val => { this._treeLoading.next(true) }),
            switchMap((aggQuery) => this.searchService.getSearchResultsByQuery(aggQuery))
        ).subscribe((res) => {
            var filterTree = this.buildTree(res.Categories.map((val) => {
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
            }));
            let selectedFilters = [];
            if (this._checkTreeKeys != undefined && this._checkTreeKeys.length > 0) {
                for (let ctk of this._checkTreeKeys) {
                    let node = this.getNodeWithKey(ctk.key, filterTree);
                    if (node) {
                        selectedFilters.push(node);
                    }
                }
                this.selectedFilters = selectedFilters;
            }
            this._categories.next(filterTree);
            this._treeLoading.next(false);
        }
        );

        //Main query - results goes in the card list
        this.MainSub$ = this.MainQuery$.pipe(
            debounceTime(this.debounceValue),
            distinctUntilChanged(this.compareQueries),
            tap(val => { this._loading.next(true); }),
            switchMap((mainQuery) => this.searchService.getSearchResultsByQuery(mainQuery))
        ).subscribe((res) => {
            this._resultCount.next(res.Result.Matches);
            this._pageNumber.next(this._query.From / this._query.Size);
            this._results.next(res.Result.Results);
            this._loading.next(false);
        });
    }

    /******* Utility functions*****************************/

    /**
     * Translates the internal d3s Class value to the display name from the Settings list of Search Types
     * @param category
     */
    private getDisplayLookup(category: string) {
        let type = this.searchTypes.find(t => t.value == category);
        return (type == undefined) ? category : type.title;
    }

    /**
     * Copied from check-tree.component.ts
     * Method to retreive a CheckTreeNode based on the key
     * @param key
     * @param nodes
     */
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

    private _baseCategoryTree: CheckTreeNode[];
    /**
     * Creates a base checkbox tree with all applicable Classes present and 0 as result count.
     * Will be merged with the aggregate result from search
     **/
    private getBaseCategoryTree() {
        if (this._baseCategoryTree == undefined) {
            this._baseCategoryTree = this.searchTypes.map((val) => {
                return {
                    "label": val.title,
                    "count": 0,
                    "type": "category",
                    "data": val.value,
                    "key": val.value
                }
            })
        }
        return this._baseCategoryTree;
    }

    /**
     * Merges an aggregate result with the base category tree to provide the CheckTreeNode[] options for the checkbox tree
     * @param aggResult
     */
    private buildTree(aggResult: CheckTreeNode[]): CheckTreeNode[] {
        let tree = [].concat(this.getBaseCategoryTree());
        aggResult.forEach(function (v, i, a) {
            let idx = tree.findIndex((f) => f.key === v.key);
            if (idx >= 0) {
                tree[idx] = v;
            } else {
                tree.push(v);
            }
        });
        return tree;
    }

    /**
     * Compares two SearchQuery objects. Used in distinctUntilChanged calls to determine if a query has changed.
     * @param x
     * @param y
     */
    compareQueries(x: SearchQuery, y: SearchQuery): boolean {
        if (y.Force)
            return false;
        if (x.Term != y.Term)
            return false;
        if (x.Size != y.Size)
            return false;
        if (x.From != y.From)
            return false;
        if (x.Explain != y.Explain)
            return false;
        if (x.Aggregations == undefined || y.Aggregations == undefined || x.Aggregations.length != y.Aggregations.length)
            return false;
        if (JSON.stringify(x.Aggregations) != JSON.stringify(y.Aggregations))
            return false;
        if (x.AggregationFilters == undefined || y.AggregationFilters == undefined || x.AggregationFilters.length != y.AggregationFilters.length)
            return false;
        if (JSON.stringify(x.AggregationFilters) != JSON.stringify(y.AggregationFilters))
            return false;
        if (x.FieldFilters == undefined || y.FieldFilters == undefined || x.FieldFilters.length != y.FieldFilters.length)
            return false;
        if (JSON.stringify(x.FieldFilters) != JSON.stringify(y.FieldFilters)) {
            return false;
        }
        return true;
    }
}