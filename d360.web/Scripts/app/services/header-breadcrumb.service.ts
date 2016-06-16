///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';
import {Subject} from 'rxjs/Subject';
import { Breadcrumb } from '../models/breadcrumb.model';

@Injectable()
export class HeaderBreadcrumbService {    
    // Observable sources
    private breadcrumbSource = new Subject<Breadcrumb>();
    private breadcrumbClearSource = new Subject<boolean>();
          
     // Observable streams
    breadcrumbs$ = this.breadcrumbSource.asObservable();
    breadcrumbClear$ = this.breadcrumbClearSource.asObservable();
      
     // Service message commands
     showBreadcrumb(breadcrumb: Breadcrumb) {
         this.breadcrumbSource.next(breadcrumb);
     }

     clearBreadcrumbs() {
         this.breadcrumbClearSource.next(true);
     }
}