///<reference path="../../es6-shim.d.ts"/>
import {Input, Output, Component, EventEmitter } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { FormMessage, JsonResult } from '../../models/form.model';
import { FormMessagePart } from '../parts/form-message.part';
import { Button } from 'primeng/primeng';

@Component({
    selector: 'delete-form',
    templateUrl: 'scripts/app/components/forms/delete.form.html',
    viewProviders: [HTTP_PROVIDERS],
    directives: [FormMessagePart, Button],
})

export class DeleteForm {
    @Input() model: any;
    @Input() uri: string;
    @Input() method: string = 'post';
    @Input() prompt: string;
    @Input() callback: Function;
    @Input() itemId: number;
    @Output() onDeleteComplete = new EventEmitter();
    @Output() onDeleteSuccess = new EventEmitter();
    @Output() onDeleteFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    private message: FormMessage = new FormMessage();
    private isLoading = false;

    http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    private delete(): void {
        if (this.isLoading)
            return;
        var headers = new Headers();
        headers.append('Content-Type', 'application/json');

        this.isLoading = true;
        switch (this.method.toLowerCase()) {
            case 'callback':
                this.callback(this.itemId);
                this.isLoading = false;
                break;
            case 'post':
                this.http.post(this.uri, JSON.stringify(this.model), { headers: headers })
                    .map(data => data.json())
                    .subscribe(
                    data => {
                        var r = new JsonResult(data);
                        if (r.isError) {
                            this.message.Error(r.message);
                            this.onDeleteFail.emit({ message: this.message });
                        } else if (r.isSuccess) {
                            this.message.Success(r.message);
                            this.onDeleteSuccess.emit({ message: this.message });
                        } else {
                            this.message.Info(r.message);
                        }
                        this.onDeleteComplete.emit({ message: this.message });
                        this.isLoading = false;
                    }
                    );
                break;
            case 'put':
                this.http.put(this.uri, JSON.stringify(this.model), { headers: headers })
                    .map(data => data.json())
                    .subscribe(
                    data => {
                        var r = new JsonResult(data);
                        if (r.isError) {
                            this.message.Error(r.message);
                            this.onDeleteFail.emit({ message: this.message });
                        } else if (r.isSuccess) {
                            this.message.Success(r.message);
                            this.onDeleteSuccess.emit({ message: this.message });
                        } else {
                            this.message.Info(r.message);
                        }
                        this.onDeleteComplete.emit({ message: this.message });
                        this.isLoading = false;
                    }
                    );
                break;
            case 'delete':
                if (this.model)
                    console.log('WARN: model passed to delete-generic will be ignored with DELETE method.');
                this.http.delete(this.uri)
                    .map(data => data.json())
                    .subscribe(
                    data => {
                        //console.log(data);
                        var r = new JsonResult(data);
                        if (r.isError) {
                            this.message.Error(r.message);
                            this.onDeleteFail.emit({ message: this.message });
                        } else if (r.isSuccess) {
                            this.message.Success(r.message);
                            this.onDeleteSuccess.emit({ message: this.message });
                        } else {
                            this.message.Info(r.message);
                        }
                        this.onDeleteComplete.emit({ message: this.message });
                        this.isLoading = false;
                    }
                    );
                break;
            default:
                console.log('method ' + this.method + ' not implemented');
                this.isLoading = false;
                break;
        }

    }

    private cancel(): void {
        this.onCancel.emit(null);
    }
}
