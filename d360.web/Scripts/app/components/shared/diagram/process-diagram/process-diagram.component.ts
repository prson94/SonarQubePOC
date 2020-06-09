import * as go from 'gojs';
import * as _ from 'lodash';
import { AfterViewInit, Component, ElementRef, HostListener, Input, OnInit, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, SimpleChange, SimpleChanges, EventEmitter, Output, AfterViewChecked } from '@angular/core';
import {
    AssetBrowserTranslation,
    AssetBrowserApiHopDirection,
    AssetBrowserDiagramAsset,
    AssetBrowserTranslationNode,
    AssetBrowserTranslationLink,
    AssetBrowserTranslationRelationCount,
    AssetBrowserFilterModel,
    FilterSelectionsModel,
    AssetBrowserApiHopRequestModel,
    AssetBrowserApiHopAssetRequestModel,
    AssetBrowserTranslationOwnerCount,
    AssetBrowserApiOwnerHopRequestModel,
    AssetBrowserAssetsModel,
    AssetBrowserModel,
    AssetBrowserAssetModel,
    AssetBrowserGenericRelationModel,
    LoadedFilterTypesModel,
    AssetBrowserApiHopType,
    AssetBrowserAlert,
    DiagramType,
    AssetBrowserFilterChangeEventType,
    AssetBrowserFilterChangeEvent,
    AssetBrowserPanelCommand,
    AssetBrowserPanelModel,

    AssetBrowserApiHopIgnoreRequestModel
} from '../../../../models/lineage.model';

import { BrowserService } from '../../../../services/browser.service';
import { PermissionsService } from '../../../../services/permissions.service';
import { MessagesObservableService } from '../../../../services/messages-observable.service';


import { DiagramBaseComponent } from '../diagram-base.component';
import { MenuItem, SelectItem, TreeNode } from 'primeng/api';
import { Observable } from 'rxjs';
import { PredicatesService } from '../../../../services/predicates.service';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { Router, ActivatedRoute } from '@angular/router';
import { SiteUrlHelpers } from '../../../../static/site-url-helpers';

declare var window: any;

@Component({
    selector: 'd3s-process-diagram',
    templateUrl: './process-diagram.component.html',
    providers: [BrowserService, PermissionsService, PredicatesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramComponent extends DiagramBaseComponent {
   

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
    ) {
        super();
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;
    }

} 