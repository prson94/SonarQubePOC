
import { Injectable } from '@angular/core';

import {Subject} from 'rxjs/Subject';

import {SiteMessage} from '../models/site-message.model';

@Injectable()
export class MessagesService {    
    // Observable sources
    private errorMessageSource = new Subject<SiteMessage>();
    private infoMessageSource = new Subject<SiteMessage>();
    
    // Observable streams
    errorMessage$ = this.errorMessageSource.asObservable();
    infoMessage$ = this.infoMessageSource.asObservable();
    
    // Service message commands
    showError(summary: string, detail: string) {        
        this.errorMessageSource.next(new SiteMessage(summary,detail));
    }

    showInfoMessage(summary: string, detail: string) {
        this.infoMessageSource.next(new SiteMessage(summary, detail));
    }
}