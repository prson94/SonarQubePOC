import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface DynEditorUpdate {
    assetUid: string,
    fieldName: string,
    fieldValue: string
}

@Injectable({
    providedIn: 'root'
})
export class DynEditorService {
    formUpdate: BehaviorSubject<DynEditorUpdate> = new BehaviorSubject(null);
    constructor() {

    }

    updateForm(data: DynEditorUpdate) {
        this.formUpdate.next(data);
    }
}