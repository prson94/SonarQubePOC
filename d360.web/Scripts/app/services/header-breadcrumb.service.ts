import { Injectable } from '@angular/core';
import {Subject} from 'rxjs';
import { Breadcrumb } from '../models/breadcrumb.model';


@Injectable()
export class HeaderBreadcrumbService {
    // Observable sources
    private breadcrumbSource = new Subject<Breadcrumb>();
    private breadcrumbClearSource = new Subject<boolean>();
    private breadcrumbTreeSource = new Subject<number>();
    private breadcrumbPopLastSource = new Subject<boolean>();
    private currentObjectInfoSource = new Subject<any>();

    // Observable streams
    breadcrumbs$ = this.breadcrumbSource.asObservable();
    breadcrumbClear$ = this.breadcrumbClearSource.asObservable();
    breadcrumbTreeSource$ = this.breadcrumbTreeSource.asObservable();
    breadcrumbPopLastSource$ = this.breadcrumbPopLastSource.asObservable();
    currentObjectInfo$ = this.currentObjectInfoSource.asObservable();

    currentObject: any;

    // Service message commands
    
    clearCurrentObjectInfo() {
        this.currentObject = { type: null, id: null };
        this.currentObjectInfoSource.next({ type: null, id: null });
    }

    setCurrentObjectInfo(type: string, id: number) {
        this.currentObject = { type: type, id: id };
        this.currentObjectInfoSource.next({ type: type, id: id });
    }

    showBreadcrumb(breadcrumb: Breadcrumb) {
        this.breadcrumbSource.next(breadcrumb);
    }

    clearBreadcrumbs() {
        this.breadcrumbClearSource.next(true);
    }

    breadcrumbTreeClick(id: number) {
        this.breadcrumbTreeSource.next(id);
    }

    popLastBreadcrumb() {
        this.breadcrumbPopLastSource.next(true);
    }
}