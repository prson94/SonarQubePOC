import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DefaultTableSettingsService {
	defaultPagingOptions: number[] = [10, 25, 50, 100];
	defaultInitialItemsPerPage = 10;

  constructor() { }
}
