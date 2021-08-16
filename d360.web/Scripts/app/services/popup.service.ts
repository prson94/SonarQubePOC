import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { LocationStrategy } from '@angular/common'

@Injectable({
    providedIn: 'root'
})
export class PopupBackButtonService {
    // Observable sources
    private backButtonSource = new Subject<string>();
    backButtonClicked = this.backButtonSource.asObservable();

    private popups: string[] = [];

    constructor(private location: LocationStrategy) {
        location.onPopState(() => {
            this.backButtonSource.next(this.popups.pop());
        });

        window.addEventListener('keydown', (event) => {
            if (event.keyCode === 27 && this.popups.length > 0) {
                history.back();
            }
        });
    }

    addState(popupUid: string) {
        var newHistoryState = "modal#" + popupUid;
        history.pushState(null, newHistoryState);
        this.popups.push(popupUid);
    }

    popState(popupUid: string) {
        if (this.popups.length > 0 && this.popups.some((x) => x === popupUid)) {
            history.back();
        }
    }
}