import { Input, Component, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { MessagesService } from '../../services/messages.service';
import { ObjectDetailService } from '../../services/object-detail.service';
import { BaseComponent } from '../shared/base.component';
import { NymType } from '../../models/object-detail.model';

@Component({
    selector: 'd3s-admin-nym-allocations',
    providers: [ObjectDetailService],
    template: `
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div *ngIf="!isLoading">
                        <table class="striped">
                            <thead>
                                <tr>
                                    <th class="permission-header"></th>
                                    <th style="width: 15%;" class="permission-header">Enabled</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr *ngFor="let nym of nyms">
                                    <td>{{nym.Name}}</td>
                                    <td> 
                                        <input type="checkbox" [disabled]="readonly" [(ngModel)]="nym.Enabled" /> 
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                        <div *ngIf="!readonly" class="pull-right" style="padding:5px">
                            <button pButton label="Save Changes" (click)="save()"></button>
                        </div>
                </div>
    `,
})

export class AdminNymAllocationsComponent extends BaseComponent implements OnChanges {    
    @Input() objectType: string;
    @Input() objectID: number;

    private nyms: NymType[] = [];
    
    constructor(private messagesService: MessagesService, private objectDetailService: ObjectDetailService) {
        super();
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.objectID > 0 && this.objectType) this.load();
    }

    private load() {
        this.isLoading = true;

        this.objectDetailService.getNymAllocations(this.objectID, this.objectType)
            .then(data => {                
                this.isLoading = false;
                this.nyms = data;
            });
    }

    private save() {
        this.objectDetailService.saveNymAllocations(this.objectID, this.objectType, this.nyms)
            .then(data => {
                this.showMessageForResult(this.messagesService, data);
            });
    }
}
