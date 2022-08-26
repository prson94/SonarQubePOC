import { Injectable } from '@angular/core';
import { IOutputData } from 'angular-split';
import { BehaviorSubject, fromEvent } from 'rxjs';
import { Observable } from 'rxjs/internal/Observable';
import { map, distinctUntilChanged  } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class SidePanelService {
  public windowInnerWidth: number;
  public windowInnerWidth$: Observable<number>;
  readonly sidePanelCloseWidth = 59;
  readonly sidePanelOpenDefaultWidth = 400;
  readonly sidePanelOpenMinWidth = 400;
  readonly panelWidthStorageKeyPrefix: string = 'side_panel_width_';
  
  constructor() {
    let windowSize$ = new BehaviorSubject(this.getWindowSize());
    fromEvent(window, 'resize').pipe(map(this.getWindowSize)).subscribe(windowSize$);
    windowSize$.pipe(
      map(windowSize => windowSize.width),
      distinctUntilChanged()
    ).subscribe((value: number) => this.windowInnerWidth = value);
  }

  getWindowSize() {
    return {
      width: window.innerWidth
      //you can sense other parameters here
    };
  }

  getSidePanelWidth(isSidePanelOpen: boolean, sidePanelStorageKey: string): number {
    const sidePanelWidthFromStorage = this.getSidePanelWidthFromStorage(sidePanelStorageKey);
    if (isSidePanelOpen) {
      return sidePanelWidthFromStorage ? sidePanelWidthFromStorage : this.sidePanelOpenDefaultWidth;
    }
    return this.sidePanelCloseWidth;
  }

  getSidePanelWidthFromStorage(sidePanelStorageKey: string): number {
    let sidePanelStorageState;
    try {
      sidePanelStorageState = JSON.parse(localStorage.getItem(this.panelWidthStorageKeyPrefix + sidePanelStorageKey));
    } catch {
      console.warn('State for key ' + this.panelWidthStorageKeyPrefix + sidePanelStorageKey + ' could not be parsed');
    }
    if(sidePanelStorageState?.panelWidth > this.windowInnerWidth / 2) {
      return this.windowInnerWidth / 2;
    }
    return sidePanelStorageState?.panelWidth;
  }

  getSidePanelMaxWidth(isSidePanelOpen: boolean): number {
    return isSidePanelOpen ? this.windowInnerWidth / 2 : this.sidePanelCloseWidth;
  }

  getSidePanelMinWidth(isSidePanelOpen: boolean): number {
    return isSidePanelOpen ? this.sidePanelOpenMinWidth : this.sidePanelCloseWidth;
  }

  onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
    const newSidePanelWidth: number = event.sizes[1] as number;
    this.saveNewSidePanelWidthToStorage(sidePanelStorageKey, newSidePanelWidth);
  }

  saveNewSidePanelWidthToStorage(sidePanelStorageKey: string, sidePanelWidth: number) {
    if (sidePanelStorageKey != null && sidePanelStorageKey.length > 0) {
      let state: any = {};
      if (sidePanelWidth) {
        state.panelWidth = sidePanelWidth;
      }
      localStorage.setItem(this.panelWidthStorageKeyPrefix + sidePanelStorageKey, JSON.stringify(state));
    }
  }
}
