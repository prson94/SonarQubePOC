import { Component, Input, OnInit, SimpleChange, Output, EventEmitter} from '@angular/core';
import { ReportTile, ReportTileTypes } from '../../../models/report.model';
import { MessagesService } from '../../../services/messages.service';
import { CompanySettingsService  } from '../../../services/settings.service';
import { BaseComponent } from '../../shared/base.component';
import * as _ from 'lodash';
import 'codemirror/mode/sql/sql.js';



@Component({
    selector: 'd3s-admin-report-tile-editor',
    providers: [CompanySettingsService],
    template: `
                <header>{{action}} Tile</header>
                <d3s-loading [isLoading]="isLoading"></d3s-loading>
                <div class="row" *ngIf="!isLoading">
                    <form (ngSubmit)="saveTile()" #tileForm="ngForm">                                                
                        <div class="col s12">
                            <div class="FieldName">Name</div>
                            <div>
                                <input required name="name" type="text" pInputText [(ngModel)]="editedTile.Name" style="width: 100%;" maxlength="250" #name="ngModel" />
                            </div>
                            <div *ngIf="name.errors && (name.dirty || name.touched)" class="alert alert-danger">
                                    <div [hidden]="!name.errors.required">Name is required</div>                            
                                    <div [hidden]="!name.errors.maxlength">Name cannot be more than 250 characters long.</div>
                            </div>                                                    
                        </div>
                        <div class="col s12">
                            <div class="FieldName">SQL</div>
                            <codemirror [(ngModel)]="editedTile.CommandText"
                                name="query"
                                [config]="baseConfig"
                                style="height:400px;">
                            </codemirror>                                                    
                        </div>                        
                        <div class="col s12" *ngIf="editedTile.ID">
                            <div class="FieldName" pTooltip="You can use this URI within a JSON-compatible reporting system to pull this data directly.">Tile URI</div>
                            <div>{{reportUrl()}}</div>
                        </div>
                        <div class="col s12">&nbsp;</div>
                        <div class="col s12">
                            <button pButton type="submit" [disabled]="!tileForm.form.valid || !editedTile?.CommandText" label="Save"></button>
                            <button pButton type="button" (click)="closeClick.emit();" label="Close"></button>
                        </div>                    
                    </form>
                </div>
                `
})

export class AdminReportTileEditorComponent extends BaseComponent implements OnInit {
    @Input() tile: ReportTile;    
    @Input() reportId: number

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    action: string = "Edit";
    urlPrefix: string;
    editedTile: ReportTile;

    private baseConfig = {
        lineNumbers: true,
        theme: 'eclipse',
        mode: 'sql'
    };
    
    constructor(private companySettingsService: CompanySettingsService) {
        super();        
    }

    ngOnInit() {
        if (this.tile != undefined)
            this.editedTile = _.cloneDeep(this.tile);
        else {
            this.editedTile = new ReportTile();
            this.editedTile.ReportTileType = ReportTileTypes.Table;
            this.editedTile.ContentAreaNumber = 1;
            this.editedTile.ReportID = this.reportId;
            this.action = "New";
        }
        
        this.load();
    }
    
    load() {
        this.isLoading = true;
        this.companySettingsService.getAuthenticationModel().
            subscribe(result => {
                this.urlPrefix = result.prefix;
                this.isLoading = false;
            });
    }

    saveTile() {
        this.saveClick.emit({ tile: this.editedTile, action: this.tile == null ? "new" : "edit" });
    }

    reportUrl(): string {
        return this.tile? `https://${this.urlPrefix}.data3sixty.com/services/deprecated/reports/${this.tile.ReportID}/${this.tile.ReportTileType}/1/tiles/${this.tile.ID}/data` : '';        
    }
}


