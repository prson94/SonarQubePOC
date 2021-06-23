import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class TooltipSingletonService {
    // Observable sources
    private tooltipSource = new Subject<any>();

    // Observable streams
    tooltipMessage$ = this.tooltipSource.asObservable();

    // Service message commands
    tooltipShow(objectType: string, objectId: number) {
        this.tooltipSource.next({ objectType: objectType, objectId: objectId });
    }
}