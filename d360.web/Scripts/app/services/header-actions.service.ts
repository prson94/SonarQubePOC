///<reference path="../es6-shim.d.ts"/>
import { Injectable } from '@angular/core';

import {Subject} from 'rxjs/Subject';

@Injectable()
export class HeaderActionsService {    
    // Observable sources
    showNotifications: boolean = true;
    showHelp: boolean = true;
    showSearch: boolean = true;
    showRaiseIssue: boolean = false;   
}