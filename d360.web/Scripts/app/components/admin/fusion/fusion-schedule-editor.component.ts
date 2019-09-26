import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { FusionSchedule } from '../../../models/fusion.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-fusion-schedule-editor',
    template: ` 
                <header>{{action}} Schedule</header>                
                <div class="row">
                    <form (ngSubmit)="onSubmit()" #scheduleForm="ngForm">                        
                        <div class="col s12">
                            <div class="FieldName">Day</div>
                            <div>
                                <select required name="days" style="width:100%;" placeholder="Choose a value" [(ngModel)]="editedSchedule.Day" #day="ngModel">                                            
                                    <option></option>
                                    <option value="0">Sunday</option>
                                    <option value="1">Monday</option>
                                    <option value="2">Tuesday</option>
                                    <option value="3">Wednesday</option>
                                    <option value="4">Thursday</option>
                                    <option value="5">Friday</option>
                                    <option value="6">Saturday</option>                                    
                                </select>
                            </div>                            
                            <div class="error" [hidden]="day.valid || day.pristine">A day of week is required.</div>
                        </div>                        
                        <div class="col s12">
                            <div class="FieldName">Time (UTC)</div>
                            <div>                                
                                <p-inputMask name="time" [(ngModel)]="editedSchedule.Time" mask="99:99" #time="ngModel"></p-inputMask>                                
                            </div>
                            <div [hidden]="time.valid || time.pristine">Time value is required.</div>
                        </div>                                  
                        <div class="col s12">
                            <div class="FieldName">Force Refresh</div>    
                            <div><input name="forcerefresh" type="checkbox" [disabled]="readonly" [(ngModel)]="editedSchedule.FullRefresh" /> </div>                        
                        </div>                                           
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!scheduleForm.form.valid" label="Save"></button>                            
                            <button pButton type="button" (click)="closeClick.emit();" label="Close"></button>
                        </div>                    
                    </form>                           
                </div>
                `,    
})

export class FusionScheduleEditorComponent extends BaseComponent implements OnInit {
    @Input() selection: FusionSchedule = null;    
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
    action: string = "Edit";    
    editedSchedule: FusionSchedule = null;
    isLoading: boolean = false;
            
    ngOnInit() {
        if (this.selection) {            
            this.editedSchedule = _.cloneDeep(this.selection);          
        }
        else {            
            this.action = "New";
            this.editedSchedule = new FusionSchedule();
        }
    }

    onSubmit() {
        //save the item back to the save or edit url        
        this.saveClick.emit({ schedule: this.editedSchedule, action: this.action == "New" ? "new" : "edit" });
    }    
};