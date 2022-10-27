import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable()
export class PreviewpopupSingletonService {
    // Observable sources
    private popupSource = new Subject<any>();

    // Observable streams
    popupMessage$ = this.popupSource.asObservable();

    // Service message commands
    popupShow(uid: string) {
        this.popupSource.next({ uid });
    }
}