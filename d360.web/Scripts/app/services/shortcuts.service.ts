import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { MessagesService } from './messages.service';
import { BaseService } from './base.service';
import { JsonResult } from '../models/jsonresult.model';
import { Shortcut } from '../models/shortcuts.model';

@Injectable()
export class ShortcutService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }


    public addShortcut(shortcut: Shortcut): Promise<JsonResult> {
        return this.http.post('form/shortcut/add', shortcut)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    public editShortcut(shortcut: Shortcut): Promise<JsonResult> {
        return this.http.put('form/shortcut/edit', shortcut)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    public deleteShortcut(id: number): Promise<JsonResult> {
        return this.http.delete(`form/shortcut/delete/${id}`)
            .toPromise()
            .then(response => <JsonResult>response.json())
            .catch(err => this.handleError(err));
    }

    public getShortcuts(): Promise<Shortcut[]> {
        return this.http.get('form/shortcut/list')
            .toPromise()
            .then(response => <Shortcut[]>response.json())
            .catch(err => this.handleError(err));
    }

  
}