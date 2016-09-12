///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import {Subject} from 'rxjs/Subject';
import { Breadcrumb } from '../models/breadcrumb.model';

@Injectable()
export class HeaderBreadcrumbService {    
    // Observable sources
    private breadcrumbSource = new Subject<Breadcrumb>();
    private breadcrumbClearSource = new Subject<boolean>();
    private breadcrumbTreeSource = new Subject<number>();
          
     // Observable streams
    breadcrumbs$ = this.breadcrumbSource.asObservable();
    breadcrumbClear$ = this.breadcrumbClearSource.asObservable();
    breadcrumbTreeSource$ = this.breadcrumbTreeSource.asObservable();
      
     // Service message commands
     showBreadcrumb(breadcrumb: Breadcrumb) {
         this.breadcrumbSource.next(breadcrumb);
     }

     clearBreadcrumbs() {
         this.breadcrumbClearSource.next(true);
     }

     breadcrumbTreeClick(id: number) {
         this.breadcrumbTreeSource.next(id);
     }
}