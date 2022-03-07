import { Injectable, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NumberOfRowsByCategories } from '../components/assets-grid/asset-grid.component';
import { LocalStorageKey } from '../enums/localstorage.enum';
import { Breadcrumb } from '../models/breadcrumb.model';
import { AppConstants } from '../static/constants';
import { LocalStorageHelper } from '../static/localstorage-helper';
import { HeaderBreadcrumbService } from './header-breadcrumb.service';

@Injectable({
  providedIn: 'root'
})
export class NumberOfRowsByCategoryService implements OnDestroy {
  rowsPerPage: Subject<number> = new Subject<number>();
  destroy = new Subject<void>();

  constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {}

  defineNumberOfRows(defaultNumberOfRows?: number): void {
    this.setNumberOfRowsToCategory(defaultNumberOfRows);
    this.headerBreadcrumbService.breadcrumbIsSetToStorage.pipe(
      takeUntil(this.destroy)
    ).subscribe(() => {
      this.setNumberOfRowsToCategory(defaultNumberOfRows);
    });
  }

  saveNumberOfRowsByCategoryToStorage(numberOfRows: number): void {
    let numberOfRowsByCategories: NumberOfRowsByCategories = this.defineNumberOfRowsByCategories();
    let category: string = this.getCategoryFromBreadcrumbs();
    numberOfRowsByCategories[category] = numberOfRows;
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

  setNumberOfRowsToCategory(defaultNumberOfRows?: number) {
    let category: string = this.getCategoryFromBreadcrumbs();
    let isLocalStorageKeyExist: boolean = LocalStorageHelper.isLocalStorageKeyExist(LocalStorageKey.NumberOfRowsByCategories);
    if (category && isLocalStorageKeyExist) {
      this.rowsPerPage.next(this.defineNumberOfRowsByCategory(category));
      console.log("this.rowsPerPage");
      console.log(this.defineNumberOfRowsByCategory(category));
    } else {
      this.rowsPerPage.next(defaultNumberOfRows || AppConstants.DEFAULT_ROWS_PER_PAGE);
    }
  }

  defineNumberOfRowsByCategory(category: string): number {
    let numberOfRowsByCategories: NumberOfRowsByCategories = this.getNumberOfRowsByCategoriesFromStorage();
    if (numberOfRowsByCategories.hasOwnProperty(category)) {
      return numberOfRowsByCategories[category];
    } else {
      return AppConstants.DEFAULT_ROWS_PER_PAGE;
    }
  }

  getNumberOfRowsByCategoriesFromStorage(): NumberOfRowsByCategories {
    return JSON.parse(localStorage.getItem(LocalStorageKey.NumberOfRowsByCategories));
  }

  ngOnDestroy() {
    this.destroy.next();
    this.destroy.complete();
  }
}
