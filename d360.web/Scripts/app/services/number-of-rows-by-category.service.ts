import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { take } from 'rxjs/operators';
import { LocalStorageKey } from '../enums/localstorage.enum';
import { Breadcrumb } from '../models/breadcrumb.model';
import { AppConstants } from '../static/constants';
import { LocalStorageHelper } from '../static/localstorage-helper';
import { HeaderBreadcrumbService } from './header-breadcrumb.service';
import { set } from "lodash";

export interface OnPageEvent {
  first: number;
  rows: number;
}

export interface NumberOfRowsByCategories {
  [category: string]: NumberOfRowsByCategories | number;
}

@Injectable({
  providedIn: 'root'
})
export class NumberOfRowsByCategoryService implements OnDestroy {
  rowsPerPage: Subject<number | NumberOfRowsByCategories> = new Subject();
  destroy = new Subject<void>();

  constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {}

  defineNumberOfRows(defaultNumberOfRows?: number): void {
    this.setNumberOfRowsToCategory(defaultNumberOfRows);
    this.headerBreadcrumbService.breadcrumbIsSetToStorage.pipe(
      take(1)
    ).subscribe(() => {
      this.setNumberOfRowsToCategory(defaultNumberOfRows);
    });
  }

  saveNumberOfRowsByCategoryToStorage(numberOfRows: number, area?: string): void {
    let numberOfRowsByCategories: NumberOfRowsByCategories = this.defineNumberOfRowsByCategories();
    let category: string = this.getCategoryFromBreadcrumbs();
    if (area) {
      set(numberOfRowsByCategories, `${category}.${area}`, numberOfRows);
    } else {
      numberOfRowsByCategories[category] = numberOfRows; // eslint-disable-line
    }
    localStorage.setItem(LocalStorageKey.NumberOfRowsByCategories, JSON.stringify(numberOfRowsByCategories));
  }

  defineNumberOfRowsByCategories(): NumberOfRowsByCategories {
    if (LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfRowsByCategories)) {
      return this.getNumberOfRowsByCategoriesFromStorage();
    } else {
      return {};
    }
  }

  getCategoryFromBreadcrumbs(): string {
    let breadcrumb: Breadcrumb[] = this.headerBreadcrumbService.getBreadcrumbsFromStorage();
    if (breadcrumb && breadcrumb[0]) {
      return breadcrumb[0].text;
    } else {
      return void(0);
    }
  }

  setNumberOfRowsToCategory(defaultNumberOfRows?: number) {
    let category: string = this.getCategoryFromBreadcrumbs();
    let isLocalStorageKeyExist: boolean = LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfRowsByCategories);
    if (category && isLocalStorageKeyExist) {
      this.rowsPerPage.next(this.defineNumberOfRowsByCategory(category, defaultNumberOfRows));
    } else {
      this.rowsPerPage.next(defaultNumberOfRows || AppConstants.DEFAULT_ROWS_PER_PAGE);
    }
  }

  defineNumberOfRowsByCategory(category: string, defaultNumberOfRows?: number): NumberOfRowsByCategories | number {
    let numberOfRowsByCategories: NumberOfRowsByCategories = this.getNumberOfRowsByCategoriesFromStorage();
    if (numberOfRowsByCategories.hasOwnProperty(category)) {
      return numberOfRowsByCategories[category]; // eslint-disable-line
    } else {
      return defaultNumberOfRows || AppConstants.DEFAULT_ROWS_PER_PAGE;
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
