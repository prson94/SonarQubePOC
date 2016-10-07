import { Input, Output, Component, EventEmitter } from '@angular/core';
import { Http, Headers } from '@angular/http';
import { FormMessage} from '../../models/form.model';
import { JsonResult } from '../../models/jsonresult.model';

@Component({
    selector: 'delete-form',
    templateUrl: './delete.form.html',    
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
                    console.warn('Model passed to generic delete will be ignored when method=\'DELETE\'.');
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
                console.warn('Method \'' + this.method + '\' not implemented');
                this.isLoading = false;
                break;
        }

    }

    private cancel(): void {
        this.onCancel.emit(null);
    }
}
