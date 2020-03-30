import * as _ from 'lodash';
import { Component, ChangeDetectionStrategy, Output, EventEmitter, Input, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { MessagesObservableService } from '../../../../../services/messages-observable.service';
import { AssetBrowserPanelCommand } from '../../../../../models/lineage.model';

@Component({
    selector: 'd3s-assetbrowser-panels',
    templateUrl: './panels.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserPanelsComponent implements OnChanges {
    @Input() commandToResetTo: AssetBrowserPanelCommand;
    @Input() enableAdd: boolean;
    @Input() enableInformation: boolean;
    @Input() isFullScreen: boolean;
    @Input() totalAlertCount: number;
    @Output() refresh: EventEmitter<boolean> = new EventEmitter();
    @Output() download: EventEmitter<boolean> = new EventEmitter();
    @Output() click: EventEmitter<AssetBrowserPanelCommand> = new EventEmitter();    

    constructor(
        protected messagesService: MessagesObservableService
    ) {
        
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["commandToResetTo"]) {
            this.reset_Selected();
            if (this.commandToResetTo == AssetBrowserPanelCommand.Information) {
                this.selected_Information = true;
            }
        }
    }

    private alert_ButtonWidth() {
        let width: number = 32;
        if (this.totalAlertCount > 0) {
            width += (this.totalAlertCount.toLocaleString().length * 6);
            width += 10;
        }
        return width + 'px';
    }

    private alert_CountClass() {
        return this.totalAlertCount > 0 ? "fa fa-bell has-alerts-label" : "fa fa-bell";
    }

    private alert_CountNumber() {
        return this.totalAlertCount > 0 ? this.totalAlertCount : "";
    }

    private alert_CountNumberClass() {
        return this.totalAlertCount > 0 ? "has-alerts-count" : "";
    }

    private button_Css(selected: boolean, disabled: boolean) {
        let css: string = "icon mr8 " + (selected ? "selected" : "");        
        if (disabled) {
            css += " disabled";
        }
        else {
            css += " dark";
        }
        return css;
    }

    private execute(command: AssetBrowserPanelCommand) {
        this.click.emit(command);
    }

    private reset_Selected() {
        this.selected_Add = false;
        this.selected_Alert = false;
        this.selected_Filter = false;
        this.selected_Information = false;
        this.selected_Settings = false;
    }

    selected_Add: boolean = false;
    private button_Css_Add() {
        return this.button_Css(this.selected_Add, !this.enableAdd);
    }
    private execute_Add() {
        this.reset_Selected();
        this.selected_Add = !this.selected_Add;
        this.execute(AssetBrowserPanelCommand.Add);
    }

    selected_Alert: boolean = false;
    private button_Css_Alert() {
        return this.button_Css(this.selected_Alert, this.totalAlertCount == 0);
    }
    private execute_Alert() {
        this.reset_Selected();
        this.selected_Alert = !this.selected_Alert;
        this.execute(AssetBrowserPanelCommand.Alerts);
    }

    selected_Filter: boolean = false;
    private button_Css_Filter() {
        return this.button_Css(this.selected_Filter, false);
    }
    private execute_Filter() {
        this.reset_Selected();
        this.selected_Filter = !this.selected_Filter;
        this.execute(AssetBrowserPanelCommand.Filters);
    }

    selected_Information: boolean = false;
    private button_Css_Information() {
        return this.button_Css(this.selected_Information, !this.enableInformation);
    }
    private execute_Information() {
        this.reset_Selected();
        this.selected_Information = !this.selected_Information;
        this.execute(AssetBrowserPanelCommand.Information);
    }

    selected_Settings: boolean = false;
    private button_Css_Settings() {
        return this.button_Css(this.selected_Settings, false);
    }
    private execute_Settings() {
        this.reset_Selected();
        this.selected_Settings = !this.selected_Settings;
        this.execute(AssetBrowserPanelCommand.Settings);
    }
} 