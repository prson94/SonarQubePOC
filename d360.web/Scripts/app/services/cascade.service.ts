import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { CascadingChange } from '../models/cascade.model';
import { distinctUntilChanged } from 'rxjs/operators';

@Injectable({
    providedIn: 'root'
})
export class CascadeService {
    // Observable sources
    private cascadeSource = new Subject<CascadingChange>();
    
    // Observable streams
    cascadeMessage$ = this.cascadeSource.asObservable().pipe(
        distinctUntilChanged((c, p) => c.fieldTypeId === p.fieldTypeId && c.parentListItemId === p.parentListItemId)
    );
    
    // Service message commands
    cascadeEvent(fieldTypeId: number, parentListId: string) {
        this.cascadeSource.next(new CascadingChange(fieldTypeId, parentListId));
    }
}