import * as _ from 'lodash';
import { Component, ChangeDetectionStrategy, Output, EventEmitter, Input } from '@angular/core';
import { AssetBrowserPanelCommand, DiagramType } from '../../../../../models/lineage.model';

@Component({
    selector: 'd3s-assetbrowser-panels',
    templateUrl: './panels.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserPanelsComponent {
    @Input() enableAdd: boolean;
    @Input() enableDownload: boolean;
    @Input() enableInformation: boolean;

    @Input() addSelected: boolean;
    @Input() alertsSelected: boolean;
    @Input() filtersSelected: boolean;
    @Input() informationSelected: boolean;
    @Input() settingsSelected: boolean;

    @Input() isFullScreen: boolean;
    @Input() totalAlertCount: number;

    @Input() diagramType = DiagramType.Lineage;

    @Output() refresh: EventEmitter<boolean> = new EventEmitter();
    @Output() download: EventEmitter<boolean> = new EventEmitter();
    @Output() click: EventEmitter<AssetBrowserPanelCommand> = new EventEmitter();

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

    execute(command: AssetBrowserPanelCommand) {
        this.click.emit(command);
    }

    private execute_Add() {
        this.execute(AssetBrowserPanelCommand.Add);
    }

    private execute_Alert() {
        this.execute(AssetBrowserPanelCommand.Alerts);
    }

    private execute_Filter() {
        this.execute(AssetBrowserPanelCommand.Filters);
    }

    private execute_Information() {
        this.execute(AssetBrowserPanelCommand.Information);
    }

    private execute_Settings() {
        this.execute(AssetBrowserPanelCommand.Settings);
    }

    get expandShringLabel(): string {
        return this.isFullScreen ? $localize`Shrink View` : $localize`Expand View`;
    }
} 