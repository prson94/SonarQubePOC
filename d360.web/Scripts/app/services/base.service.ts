import { Injectable } from '@angular/core';

import 'rxjs/add/operator/toPromise';
import { MessagesService } from './messages.service';

@Injectable()
export class BaseService {
    
    constructor(protected messages: MessagesService) {  }

    handleError(error: any) {
        console.error('An error occurred', error);        
        this.messages.showError('Error', error.statusText);
        return Promise.reject(error.message || error);
    }
}