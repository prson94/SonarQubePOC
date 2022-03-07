import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NumberOfRowsByCategories, OnPageEvent } from '../components/assets-grid/asset-grid.component';
import { LocalStorageKey } from '../enums/localstorage.enum';
import { Breadcrumb } from '../models/breadcrumb.model';
import { AppConstants } from '../static/constants';
import { LocalStorageHelper } from '../static/localstorage-helper';
import { HeaderBreadcrumbService } from './header-breadcrumb.service';
import { set, get } from "lodash";

@Injectable({
  providedIn: 'root'
})
export class NumberOfRowsByCategoryService implements OnDestroy {
  rowsPerPage: Subject<number> = new Subject<number>();
  destroy = new Subject<void>();

  constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {}

  defineNumberOfRows(defaultNumberOfRows?: number, area?: string): void {
    this.setNumberOfRowsToCategory(defaultNumberOfRows, area);
    this.headerBreadcrumbService.breadcrumbIsSetToStorage.pipe(
      takeUntil(this.destroy)
    ).subscribe(() => {
      this.setNumberOfRowsToCategory(defaultNumberOfRows, area);
    });
  }

  saveNumberOfRowsByCategoryToStorage(numberOfRows: number, area?: string): void {
    let numberOfRowsByCategories: NumberOfRowsByCategories = this.defineNumberOfRowsByCategories();
    let category: string = this.getCategoryFromBreadcrumbs();
    if (area) {
      set(numberOfRowsByCategories, `${category}.${area}`, numberOfRows);
    } else {
      numberOfRowsByCategories[category] = numberOfRows;
    }
    localStorage.setItem(LocalStorageKey.NumberOfRowsByCategories, JSON.stringify(numberOfRowsByCategories));
  }

  defineNumberOfRowsByCategories(): NumberOfRowsByCategories {
    if (LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfRowsByCategories)) {
      return this.getNumberOfRowsByCategoriesFromStorage();
    } else {
      return {}
    }
  }

  getCategoryFromBreadcrumbs(): string {
    let breadcrumb: Breadcrumb[] = this.headerBreadcrumbService.getBreadcrumbsFromStorage();
    if (breadcrumb && breadcrumb[0]) {
      return breadcrumb[0].text;
    } else {
      return undefined;
    }
  }

  setNumberOfRowsToCategory(defaultNumberOfRows?: number, area?: string) {
    let category: string = this.getCategoryFromBreadcrumbs();
    let isLocalStorageKeyExist: boolean = LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfRowsByCategories);
    if (category && isLocalStorageKeyExist) {
      this.rowsPerPage.next(this.defineNumberOfRowsByCategory(category, area));
      console.log("this.rowsPerPage");
      console.log(this.defineNumberOfRowsByCategory(category, area));
    } else {
      this.rowsPerPage.next(defaultNumberOfRows || AppConstants.DEFAULT_ROWS_PER_PAGE);
    }
  }

  defineNumberOfRowsByCategory(category: string, area?: string): number {
    let numberOfRowsByCategories: NumberOfRowsByCategories = this.getNumberOfRowsByCategoriesFromStorage();
    if (numberOfRowsByCategories.hasOwnProperty(category)) {
      if (area) {
        return get(numberOfRowsByCategories, [category, area], AppConstants.DEFAULT_ROWS_PER_PAGE);
      } else {
        return numberOfRowsByCategories[category];
      }
    } else {
      return AppConstants.DEFAULT_ROWS_PER_PAGE;
    }
  }

  getNumberOfRowsByCategoriesFromStorage(): NumberOfRowsByCategories {
    return JSON.parse(localStorage.getItem(LocalStorageKey.NumberOfRowsByCategories));
  }

  onPage(event: OnPageEvent, area?: string): void {
    this.saveNumberOfRowsByCategoryToStorage(event.rows, area);
  }

  ngOnDestroy() {
    this.destroy.next();
    this.destroy.complete();
  }
}
