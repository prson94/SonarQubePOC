///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output } from '@angular/core';
import { LookupService } from '../../services/index';
import { Lookup } from '../../models/lookup.model';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-admin-lookup-type-editor',
    template: ` 
                <header>{{action}} Lookup</header>
                <div class="row">
                    <div class="col l6 s12">
                        <div class="FieldName">Name</div>
                        <div><input type="text" pInputText [(ngModel)]="editedLookup.Name" style="width: 100%;" /></div>
                    </div>                    
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="button" (click)="update()" label="Save" style="width: '150px';"></button>
                        <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                    </div>                    
                </div>
                `,
    providers: [LookupService],
})

export class AdminLookupTypeEditorComponent {    
    @Input() lookup: Lookup;
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    action: string = "Edit";
    
    editedLookup: Lookup;
    
    constructor(private lookupService: LookupService) { }

    ngOnInit() {
        if (this.lookup != undefined)
            this.editedLookup = _.cloneDeep(this.lookup);
        else {
            this.editedLookup = new Lookup();
            this.action = "New";
        }        
    }
    
    update() {
        this.saveClick.emit({ lookup: this.editedLookup, action: this.lookup == null ? "new" : "edit" });
    }    
};