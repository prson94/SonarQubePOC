import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/Subject';
import { CascadingChange } from '../models/cascade.model';

@Injectable()
export class CascadeService {
    // Observable sources
    private cascadeSource = new Subject<CascadingChange>();
    
    // Observable streams
    cascadeMessage$ = this.cascadeSource.asObservable();
    
    // Service message commands
    cascadeEvent(fieldTypeId: number, parentListId: number) {
        this.cascadeSource.next(new CascadingChange(fieldTypeId, parentListId));
    }
}