import { Injectable } from '@angular/core';
import { IOutputData } from 'angular-split';
import { BehaviorSubject, fromEvent, Subject } from 'rxjs';
import { Observable } from 'rxjs/internal/Observable';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { MessagesObservableService } from './messages-observable.service';

export class SidePanelState {
	expanded: boolean;
}

@Injectable({
	providedIn: 'root'
})
export class SidePanelService {
	public windowInnerWidth: number;
	public windowInnerWidth$: Observable<number>;
	readonly sidePanelCloseWidth = 59;
	readonly sidePanelOpenDefaultWidth = 400;
	readonly sidePanelOpenMinWidth = 400;
	readonly draggableAreaWidth: number = 6;
	readonly panelWidthStorageKeyPrefix: string = 'side_panel_width_';

	private sidePanelStateSource = new Subject<SidePanelState>();
	sidePanelStateChange$ = this.sidePanelStateSource.asObservable();

	private editClickSource = new Subject<unknown>();
	editClickSource$ = this.editClickSource.asObservable();

	private refreshSource = new Subject<void>();
	refreshSource$ = this.refreshSource.asObservable();

	constructor(private messagesService: MessagesObservableService) {
		const windowSize$ = new BehaviorSubject(this.getWindowSize());
		fromEvent(window, 'resize').pipe(map(this.getWindowSize)).subscribe(windowSize$);
		windowSize$.pipe(
			map((windowSize) => windowSize.width),
			distinctUntilChanged()
		).subscribe((value: number) => this.windowInnerWidth = value);
	}

	getWindowSize() {
		return {
			width: window.innerWidth
		};
	}

	getSidePanelWidth(isSidePanelOpen: boolean, sidePanelStorageKey: string, options?: { sidePanelCloseCustomWidth: number }): number {
		const sidePanelWidthFromStorage = this.getSidePanelWidthFromStorage(sidePanelStorageKey);
		if (isSidePanelOpen) {
			return sidePanelWidthFromStorage ? sidePanelWidthFromStorage : this.sidePanelOpenDefaultWidth;
		}
		if (options?.hasOwnProperty('sidePanelCloseCustomWidth')) {
			return options.sidePanelCloseCustomWidth;
		}
		return this.sidePanelCloseWidth;
	}

	getSidePanelWidthFromStorage(sidePanelStorageKey: string): number {
		let sidePanelStorageState;
		try {
			sidePanelStorageState = JSON.parse(localStorage.getItem(this.panelWidthStorageKeyPrefix + sidePanelStorageKey));
		} catch (e) {
			this.messagesService.showError('State for key ' + this.panelWidthStorageKeyPrefix + sidePanelStorageKey + ' could not be parsed', e);
		}
		if (sidePanelStorageState?.panelWidth > this.windowInnerWidth / 2) {
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
			const state: any = {};
			if (sidePanelWidth) {
				state.panelWidth = sidePanelWidth;
			}
			localStorage.setItem(this.panelWidthStorageKeyPrefix + sidePanelStorageKey, JSON.stringify(state));
		}
	}

	public setSidePanelState(state: SidePanelState) {
		this.sidePanelStateSource.next(state);
	}

	public editClick(event) {
		this.editClickSource.next(event);
	}

	public refreshSidePanel() {
		this.refreshSource.next();
	}
}
