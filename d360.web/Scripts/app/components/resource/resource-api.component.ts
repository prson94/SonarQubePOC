import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { ResourceAPICredentials } from '../../models/resource.model';
import { ResourcesService } from '../../services/resources.service';

declare var CurrentResourceID;

@Component({
    selector: 'd3s-resource-api',
    template: `
<header>
Your Api Credentials
<div *ngIf="hasCloseButton" (click)="onClose.emit()" style="cursor: pointer; float: right; font-size: 1.3em"><i class="fa fa-remove"></i></div>
</header>
<div *ngIf="isLoading">
</div>
<div *ngIf="!isLoading">    
            <div class="row">
                <div class="col s12">
                    <h4>CRUD API</h4>
                    <div class="form-instructions">
                        Use this set of credentials when attempting to read, add, update, or delete any information within your environment.
                        Responsibilities still apply to your account so you may not have access to change certain objects.
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <div>
                        <div class="FieldName">Api Key</div>
                        <input pInput type="text" [value]="resource.PublicKey" readonly  style="width: 100%" />
                    </div>
                    <div>
                        <div class="FieldName">Api Secret</div>
                        <input pInput type="text" [value]="resource.PrivateKey" readonly style="width: 100%" />
                    </div>
                </div>
            </div>    
</div>
`,
    providers: [ResourcesService]
})

export class ResourceApiComponent extends BaseComponent {
    @Input() hasCloseButton = false;
    @Output() onClose = new EventEmitter();

    private resource: ResourceAPICredentials;

    constructor(private resourcesService: ResourcesService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.resourcesService.getMyCredentials()
            .subscribe(r => {
                this.resource = r;
                this.isLoading = false;
            });
    }
}