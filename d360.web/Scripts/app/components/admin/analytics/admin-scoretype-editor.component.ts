import { Input, Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { StatisticService } from '../../../services/statistics.service';
import { ScoreType } from '../../../models/statistic.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-scoretype-editor',
    template: ` 
                <header>{{action}} Score Type</header>
                <form (ngSubmit)="onSubmit()" #modelForm="ngForm">
                <div class="row">
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input required type="text" name="name" pInputText [(ngModel)]="editedScoreType.Name" style="width: 100%;" #name="ngModel" maxlength="250" /></div>
                        <div *ngIf="name.errors && (name.dirty || name.touched)"
                             class="alert alert-danger">
                            <div [hidden]="!name.errors.required" style="color: maroon">
                              A Model name is required
                            </div>                            
                            <div [hidden]="!name.errors.maxlength" style="color: maroon">
                              A Model name cannot be more than 250 characters long.
                            </div>
                        </div>      
                    </div>
                    <div class="col s12">
                        <div class="FieldName">Description</div>
                        <p-editor [style]="{'height':'150px'}" name="description" [(ngModel)]="editedScoreType.Description"></p-editor>
                    </div>                    
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!modelForm.form.valid" label="Save" style="width: 150px;"></button>
                        <button pButton type="button" (click)="close()" label="Close" style="width: 150px;"></button>
                    </div>                    
                </div>
                </form>
                `,
    providers: [StatisticService],
})

export class AdminScoreTypeEditorComponent {
    @Input() scoretype: ScoreType;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";
    error: any;
    editedScoreType: ScoreType;
    
    constructor(private statisticService: StatisticService) {
        
    }

    ngOnInit() {        
        if (this.scoretype != undefined)
            this.editedScoreType = _.cloneDeep(this.scoretype);
        else {
            this.editedScoreType = new ScoreType();
            this.action = "New";
        }
    }

    onSubmit() {
        this.saveClick.emit({ scoretype: this.editedScoreType, action: this.editedScoreType.ID == undefined ? "new" : "edit" });
    }

    close() {
        this.closeClick.emit({ scoreTypeId: (this.scoretype ? this.scoretype.ID : -1) });
    }

};