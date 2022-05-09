import { APP_INITIALIZER, Injectable } from '@angular/core';
import { BehaviorSubject, interval } from 'rxjs';
import { distinctUntilChanged, filter, map, shareReplay, take } from 'rxjs/operators';
import { LocalStorageKey } from '../enums/localstorage.enum';
import { Breadcrumb } from '../models/breadcrumb.model';
import { AppConstants } from '../static/constants';
import { LocalStorageHelper } from '../static/localstorage-helper';
import { HeaderBreadcrumbService } from './header-breadcrumb.service';
import { cloneDeep, isEqual, get, set } from "lodash";
import { NavigationEnd, Router } from '@angular/router';

export interface OnPageEvent {
  first: number;
  rows: number;
}

export interface ListToNumberOfRows {
  [list: string]: number;
}

export interface PageCategoryToListToNumberOfRows {
  [pageCategory: string]: ListToNumberOfRows;
}

type PendingChange
  = { type: 'setDefaultRowsPerPage', list: string, defaultNumberOfRows: number }
  | { type: 'setNumberOfRows', list: string, numberOfRows: number };

interface State {
  pageToListToNumberOfRows: PageCategoryToListToNumberOfRows;

  currentPageCategory: string | undefined;
  latestUrl: string;

  /**
   * Pending changes is a list of changes that should be applied when `currentPageCategory` is known
   * `currentPageCategory` is known only after some time, because we relay on breadcrumbs to calculate that
   */
  pendingChanges: PendingChange[];
}

@Injectable({
  providedIn: 'root'
})
export class NumberOfRowsByCategoryService {
  private state$ = new BehaviorSubject<State>({
    pageToListToNumberOfRows: this.defineNumberOfRowsByCategories(),
    currentPageCategory: undefined,
    latestUrl: '',
    pendingChanges: []
  });

  /**
   * Emits ListToNumberOfRows for current page
   */
  public rowsPerPage = this.state$.pipe(
    map((state) => {
      if (state.currentPageCategory == null) {
        const pageSettings: ListToNumberOfRows = {};
        this.applyPendingChanges(state.pendingChanges, pageSettings);
        return pageSettings;
      }

      return state.pageToListToNumberOfRows[state.currentPageCategory];
    }),
    shareReplay(1),
    filter((listToNumberOfRows) => listToNumberOfRows != null)
  );

  constructor(
    private headerBreadcrumbService: HeaderBreadcrumbService,
    private router: Router) {
  }

  /**
   * Asks to set default number of rows for given list
   * If list already have number of rows defined, we don't apply it
   */
  public defineNumberOfRows(numberOfRows?: number, list = 'Main'): void {
    this.mutateState(nextState => {
      nextState.pendingChanges.push({
        type: 'setDefaultRowsPerPage',
        list,
        defaultNumberOfRows: numberOfRows ?? AppConstants.DEFAULT_ROWS_PER_PAGE
      });
    });
  }

  /**
   * Asks to set number of rows for given list
   */
  public onPage(event: OnPageEvent, list: string = 'Main'): void {
    this.mutateState(nextState => {
      nextState.pendingChanges.push({ type: 'setNumberOfRows', list, numberOfRows: event.rows });
    });
  }

  public ngOnInit() {
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      // After some redirects we still stay on same page with same components, so we shouldn't reset pendingChanges
      // Typically this happens for urls like '/reference' → '/reference;referenceListId=…'
      if (event.urlAfterRedirects.startsWith(this.state$.value.latestUrl)) {
        this.mutateState((nextState) => {
          nextState.latestUrl = event.urlAfterRedirects;
        });
        return;
      }

      this.mutateState((nextState) => {
        nextState.currentPageCategory = undefined;
        nextState.pendingChanges = [];
        nextState.latestUrl = event.urlAfterRedirects;
      });
    });


    this.headerBreadcrumbService.breadcrumbIsSetToStorage.subscribe(() => {
      const pageCategory = this.getPageCategoryFromBreadcrumbs();
      this.mutateState((nextState) => {
        nextState.currentPageCategory = pageCategory;
      });
    });

    this.state$.subscribe((state) => {
      if (!state.currentPageCategory || state.pendingChanges.length == 0) {
        return;
      }

      // setTimeout is required to preserve correct order of state$
      // otherwise rxjs emits new value before old value for other subscriptions
      setTimeout(() => {
        this.mutateState((nextState) => {
          if (nextState.pageToListToNumberOfRows[nextState.currentPageCategory] == null) {
            nextState.pageToListToNumberOfRows[nextState.currentPageCategory] = {};
          }

          const pageSettings = nextState.pageToListToNumberOfRows[nextState.currentPageCategory];
          this.applyPendingChanges(nextState.pendingChanges, pageSettings);
          nextState.pendingChanges = [];
        });
      }, 0);
    });

    this.state$.pipe(
      map((state) => state.pageToListToNumberOfRows),
      distinctUntilChanged(isEqual)
    ).subscribe((pageToListToNumberOfRows) => {
      localStorage.setItem(
        LocalStorageKey.NumberOfRowsByCategories,
        JSON.stringify(pageToListToNumberOfRows)
      );
    });
  }

  private applyPendingChanges(pendingChanges: PendingChange[], pageSettings: ListToNumberOfRows) {
    for (const pendingSave of pendingChanges) {
      if (pendingSave.type === 'setNumberOfRows') {
        pageSettings[pendingSave.list] = pendingSave.numberOfRows;
      }

      if (pendingSave.type === 'setDefaultRowsPerPage' && pageSettings[pendingSave.list] == null) {
        pageSettings[pendingSave.list] = pendingSave.defaultNumberOfRows;
      }
    }
  }

  private mutateState(mutator: (state: State) => void) {
    const nextState = cloneDeep(this.state$.value);
    mutator(nextState);
    if (!isEqual(this.state$.value, nextState)) {
      this.state$.next(nextState);
    }
  }

  private defineNumberOfRowsByCategories(): PageCategoryToListToNumberOfRows {
    if (LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfRowsByCategories)) {
      return this.getNumberOfRowsByCategoriesFromStorage();
    } else {
      return {};
    }
  }

  private getPageCategoryFromBreadcrumbs(): string {
    let breadcrumb: Breadcrumb[] = this.headerBreadcrumbService.getBreadcrumbsFromStorage();
    if (breadcrumb && breadcrumb[0]) {
      return breadcrumb[0].text;
    } else {
      return void (0);
    }
  }

  private getNumberOfRowsByCategoriesFromStorage(): PageCategoryToListToNumberOfRows {
    const storedData: OldPageCategoryToListToNumberOfRows
      = JSON.parse(localStorage.getItem(LocalStorageKey.NumberOfRowsByCategories));

    // This migration is required, because in past we stored in this way
    //    {"Reference Lists":{"Reference Lists":25},"Technical Assets":100}
    // But now it's required to be 
    //    {"Reference Lists":{"Reference Lists":25},"Technical Assets":{"Main": 100}}
    // This change happened in 2022-sprint-5 and probably can be deleted after 2022-sprint-7
    const parsedData: PageCategoryToListToNumberOfRows = {};
    for (const key of Object.keys(storedData)) {
      const value = storedData[key];
      parsedData[key] = typeof value === 'number'
        ? { 'Main': value }
        : value;
    }

    return parsedData;
  }
}

function numberOfRowsByCategoryServiceInitializer(provider: NumberOfRowsByCategoryService) {
  return () => provider.ngOnInit();
}

export const NumberOfRowsByCategoryServiceInitializer = {
  provide: APP_INITIALIZER,
  multi: true,
  useFactory: numberOfRowsByCategoryServiceInitializer,
  deps: [NumberOfRowsByCategoryService]
}

interface OldPageCategoryToListToNumberOfRows {
  [pageCategory: string]: number | ListToNumberOfRows;
} 