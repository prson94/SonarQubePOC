import { AfterViewInit, Component, Input, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, Output, OnChanges, SimpleChanges } from '@angular/core';
import {
    AssetBrowserFilterModel,
    FilterSelectionsModel,
    AssetBrowserFilterChangeEvent,
    AssetBrowserFilterChangeEventType
} from '../../../../../models/lineage.model';

import { MessagesObservableService } from '../../../../../services/messages-observable.service';

import { TreeNode } from 'primeng/api';
import { BaseComponent } from '../../../base.component';
import { CompanySettingsService } from '../../../../../services/settings.service';

declare var window: any;

@Component({
    selector: 'd3s-assetbrowser-filterpanel',
    templateUrl: './filterpanel.component.html',
    providers: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AssetBrowserFilterPanelComponent extends BaseComponent implements AfterViewInit, OnChanges {
    @Input() allowAncestry: boolean;
    @Input() options: FilterSelectionsModel;
    @Input() current: AssetBrowserFilterModel;
    @Output() apply: EventEmitter<AssetBrowserFilterChangeEvent> = new EventEmitter();

    selectedFilterAssetTypes: TreeNode[] = [];
    selectedFilterPredicates: TreeNode[] = [];
    selectedFilterResponsibilityTypes: TreeNode[] = [];

    isFilterWindowVisible: boolean = false;

    constructor(
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(settingsService);
    }

    public ngAfterViewInit() {
        this.cdRef.markForCheck();
    }

    ngOnChanges(changes: SimpleChanges): void {
        this.selectedFilterAssetTypes = this.helper_GetTreeNodeSelectionNodes(this.current.SelectedAssetTypes, this.options.FilterAssetTypes);
        this.selectedFilterPredicates = this.helper_GetTreeNodeSelectionNodes(this.current.SelectedPredicates, this.options.FilterPredicates);
        this.selectedFilterResponsibilityTypes = this.helper_GetTreeNodeSelectionNodes(this.current.SelectedResponsibilityTypes, this.options.FilterResponsibilityTypes);
    }

    /**
    * Called by the Ancestry dropdown control when selected value is updated.
    * @returns Nothing
    */
    public ancestryChange() {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.Ancestry, Model: this.current });
    }

    /**
    * Called by the asset type listbox control when an item is (un)checked.
    * @returns Nothing
    */
    assetTypeChange(e) {
        this.current.SelectedAssetTypes = this.getTreeNodeSelectionKeys(e.value);
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.AssetType, Model: this.current });
    }

    /**
    * Called by the Descendancy dropdown control when selected value is updated.
    * @returns Nothing
    */
    public descendancyChange() {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.Descendancy, Model: this.current });
    }

    /**
    * Called by the Hop Count dropdown control when selected value is updated.
    * @returns Nothing
    */
    private numberOfImpactHopsChange() {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.ImpactHopCount, Model: this.current });
    }

    /**
    * Called by the Hop Count dropdown control when selected value is updated.
    * @returns Nothing
    */
    private numberOfLineageHopsChange() {
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.LineageHopCount, Model: this.current });
    }

    /**
    * Called by the predicate listbox control when an item is (un)checked.
    * @returns Nothing
    */
    predicateChange(e) {
        this.current.SelectedPredicates = this.getTreeNodeSelectionKeys(e.value);
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.Predicate, Model: this.current });
    }

    /**
    * Called by the responsibility type listbox control when an item is (un)checked.
    * @returns Nothing
    */
    responsibilityTypeChange(e) {
        this.current.SelectedResponsibilityTypes = this.getTreeNodeSelectionKeys(e.value);
        this.apply.emit({ Type: AssetBrowserFilterChangeEventType.ResponsibilityType, Model: this.current });
    }

    private filterButtonSelectedClass() {
        return "icon right-margin-4 " + (this.isFilterWindowVisible ? "selected" : "");
    }

    private getTreeNodeSelectionNodes(keys: number[], source: TreeNode[]) {
        let nodes: TreeNode[] = [];
        source.forEach(s => {
            if (keys.indexOf(s.data) != -1) {
                nodes.push(s);
            }
            if (s.children != null && s.children.length > 0) {
                let childNodes = this.getTreeNodeSelectionNodes(keys, s.children);
                if (childNodes != null && childNodes.length > 0) {
                    nodes = nodes.concat(childNodes);
                }
            }
        });

        return nodes;
    }

    private getTreeNodeSelectionKeys(selection: TreeNode[]): number[] {
        let keys: number[] = [];

        selection.forEach(s => {
            keys.push(+s.data);
        });

        return keys;
    }

    private helper_GetTreeNodeSelectionNodes(keys: number[], source: TreeNode[]) {
        let nodes: TreeNode[] = [];
        source.forEach(s => {
            if (keys.indexOf(s.data) != -1) {
                nodes.push(s);
            }
            if (s.children != null && s.children.length > 0) {
                let childNodes = this.helper_GetTreeNodeSelectionNodes(keys, s.children);
                if (childNodes != null && childNodes.length > 0) {
                    nodes = nodes.concat(childNodes);
                }
            }
        });

        return nodes;
    }
} 