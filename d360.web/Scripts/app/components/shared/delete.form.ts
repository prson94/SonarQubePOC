
import {map} from 'rxjs/operators';
import { CommonModule }       from '@angular/common';
import { NgModule, Input, Output, Component, EventEmitter } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { FormMessage } from '../../models/form.model';
import { JsonResult } from '../../models/jsonresult.model';
import { SharedFormMessageModule } from './form-message.part';

@Component({
    selector: 'd3s-delete-form',
    templateUrl: './delete.form.html',    
})

export class DeleteForm  {
    @Input() model: any;
    @Input() uri: string;
    @Input() deleteButtonText: string;
    @Input() method: string = 'post';
    @Input() prompt: string;
    @Input() callback: Function;
    @Input() itemId: number;
    @Input() items: any[];
    @Output() onDeleteComplete = new EventEmitter();
    @Output() onDeleteSuccess = new EventEmitter();
    @Output() onDeleteFail = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    //Modal
    @Input() showAsModal: boolean = false;
    @Input() modalTitle: string = '';



    public message: FormMessage = new FormMessage();
    public isLoading = false;

    http: HttpClient;

    constructor(http: HttpClient) {
        this.http = http;
    }

    public delete(): void {
       if (this.isLoading)
            return;
        var headers = new HttpHeaders();
        headers.append('Content-Type', 'application/json');

        this.isLoading = true;
        switch (this.method.toLowerCase()) {
            case 'callback':
                if (this.items) {
                    this.callback(this.items);
                }
                else {
                    this.callback(this.itemId);
                }
                 break;
            case 'post':
                this.http.post(this.uri, JSON.stringify(this.model), { headers: headers }).pipe(
                    map(data => data))
                    .subscribe(
                    data => {
                        var r = new JsonResult(data);
                        if (r.isError) {
                            this.message.Error(r.message);
                            this.onDeleteFail.emit({ message: this.message, result: r  });
                        } else if (r.isSuccess) {
                            this.message.Success(r.message);
                            this.onDeleteSuccess.emit({ message: this.message, result: r  });
                        } else {
                            this.message.Info(r.message);
                        }
                        this.onDeleteComplete.emit({ message: this.message, result: r  });
                        this.isLoading = false;
                    }
                    );
                break;
            case 'put':
                this.http.put(this.uri, JSON.stringify(this.model), { headers: headers }).pipe(
                    map(data => data))
                    .subscribe(
                    data => {
                        var r = new JsonResult(data);
                        if (r.isError) {
                            this.message.Error(r.message);
                            this.onDeleteFail.emit({ message: this.message, result: r  });
                        } else if (r.isSuccess) {
                            this.message.Success(r.message);
                            this.onDeleteSuccess.emit({ message: this.message, result: r  });
                        } else {
                            this.message.Info(r.message);
                        }
                        this.onDeleteComplete.emit({ message: this.message, result: r  });
                        this.isLoading = false;
                    }
                    );
                break;
            case 'delete':
                if (this.model)
                    console.warn('Model passed to generic delete will be ignored when method=\'DELETE\'.');
                this.http.delete(this.uri).pipe(
                    map(data => data))
                    .subscribe(
                    data => {                        
                        var r = new JsonResult(data);
                        if (r.isError) {
                            this.message.Error(r.message);
                            this.onDeleteFail.emit({ message: this.message, result: r  });
                        } else if (r.isSuccess) {
                            this.message.Success(r.message);
                            this.onDeleteSuccess.emit({ message: this.message, result: r  });
                        } else {
                            this.message.Info(r.message);
                        }
                        this.onDeleteComplete.emit({ message: this.message, result: r });
                        this.isLoading = false;
                    }
                    );
                break;
            default:
                console.warn('Method \'' + this.method + '\' not implemented');
                this.isLoading = false;
                break;
        }

    }

    public cancel(): void {
        this.onCancel.emit(null);
    }
}


import {    
    ButtonModule,    
} from 'primeng/primeng';

@NgModule({
    declarations: [
        DeleteForm,        
    ],
    exports: [
        DeleteForm,        
    ]
    , imports: [
        CommonModule,

        ButtonModule,

        SharedFormMessageModule,
    ]

})

export class SharedDeleteFormModule { }