import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SidePanelService {
  readonly sidePanelCloseWidth = 59;
  readonly sidePanelOpenDefaultWidth = 400;
  readonly sidePanelOpenMinWidth = 400;
  readonly panelWidthStorageKeyPrefix: string = 'side_panel_width';


  constructor() { }

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
    return sidePanelStorageState?.panelWidth;
  }

  getSidePanelMaxWidth(isSidePanelOpen: boolean, innerWidth: number): number {
    return isSidePanelOpen ? innerWidth / 2 : this.sidePanelCloseWidth;
  }

  getSidePanelMinWidth(isSidePanelOpen: boolean): number {
    return isSidePanelOpen ? this.sidePanelOpenMinWidth : this.sidePanelCloseWidth;
  }

  onSidePanelDragEnd(sidePanelStorageKey: string, event: { gutterNum: number; sizes: Array<number> }): void {
    const newSidePanelWidth = event.sizes[1];
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
