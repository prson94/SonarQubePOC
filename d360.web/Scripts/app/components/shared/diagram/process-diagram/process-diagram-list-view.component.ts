import { Component, ChangeDetectionStrategy, Input, DoCheck, ChangeDetectorRef, ElementRef, ViewChild, AfterViewChecked, OnChanges } from '@angular/core';
import { DiagramBaseComponent } from '../diagram-base.component';
import { SecondaryNavService } from '../../../../services/right-sidebar.service';
import { HeaderBreadcrumbService } from '../../../../services/header-breadcrumb.service';
import { SortEvent } from 'primeng/api';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
    selector: 'd3s-process-diagram-list-view',
    templateUrl: './process-diagram-list-view.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessDiagramListViewComponent extends DiagramBaseComponent implements DoCheck, AfterViewChecked {
    @Input() nodeArray: go.ObjectData[] = [];
    @Input() nodeSelection: any;
    @Input() diagram: go.Diagram;

    private rowsPerPage: number = 10;
    private tableScrollHeight: string = '500px';

    selected: go.ObjectData[] = [];
    lastSelectedIndex: number = -1;
    private nodeCount: number = 0;
    private searchValue: string = '';

    @ViewChild('dt', { static: false }) tableEl: any;

    constructor(
        secondaryNavService: SecondaryNavService,
        breadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef,
        private elRef: ElementRef
    ) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
        this.breadcrumbsService = breadcrumbService;

    }

    ngDoCheck() {
        if (this.nodeArray) {
            if (this.nodeArray.length != this.nodeCount) {
                this.nodeArrayCountChanged();
                this.nodeCount = this.nodeArray.length;
            }

        }

        if (this.nodeSelection) {
            var arr = this.nodeSelection.toArray();
            this.selected = [];
            arr.forEach(x => {
                if (x.data && x.data.key) {
                    this.selected.push(this.nodeArray.find(node => node.key == x.data.key));
                }
            })
        }
        this.cdRef.detectChanges();
    }

    ngAfterViewChecked() {
        if (document.getElementById('process-diagram-placeholder')) {
            var height = document.getElementById('process-diagram-placeholder').clientHeight;

            this.tableScrollHeight = height - 200 + 'px';
            this.cdRef.markForCheck();
        }
    }

    nodeArrayCountChanged() {
        this.nodeArray = this.nodeArray.sort((a, b) => {
            return +a.StepNo - +b.StepNo;
        })
        this.tableEl.reset();
    }


    toggleAll($event) {
        if ($event.checked) {
            var selectedParts: go.Part[] = [];
            this.getTableCurrentData().forEach(data => {
                selectedParts.push(this.getPartByKey(data.key));
            })
            this.diagram.selectCollection(selectedParts);
        }
        else {
            this.diagram.clearSelection();
        }
    }
    getPartByKey(key: string): go.Part {
        return this.diagram.findPartForKey(key);
    }

    customSort(event: SortEvent) {
        event.data.sort((data1, data2) => {
            let value1 = data1[event.field];
            let value2 = data2[event.field];
            let result = null;

            if (value1 == null && value2 != null)
                result = -1;
            else if (value1 != null && value2 == null)
                result = 1;
            else if (value1 == null && value2 == null)
                result = 0;
            else if (typeof value1 === 'string' && typeof value2 === 'string' && event.field != 'StepNo')
                result = value1.localeCompare(value2);
            else if (event.field == 'StepNo') {
                result = (+value1 < +value2) ? -1 : (+value1 > +value2) ? 1 : 0;

            }
            else
                result = (value1 < value2) ? -1 : (value1 > value2) ? 1 : 0;

            return (event.order * result);
        })

    }
    selectSingleItem(event: MouseEvent, item: go.ObjectData, element: ElementRef = null, elIndex = -1) {
        //p table options and eventing doesnt handle multiple selection well, this is custom implementation of ctrl/shift holding while selecting
        let isCheckboxClicked = false;
        if (event && event.target) {
            var target = event.target as HTMLElement;
            isCheckboxClicked = target.tagName == 'P-TABLECHECKBOX';

            if (!isCheckboxClicked) {
                target.childNodes.forEach(cn => {
                    if (cn.nodeName === 'P-TABLECHECKBOX') {
                        isCheckboxClicked = true;
                    }
                })
            }
        }

        if (event && element) {
            if ((event.ctrlKey || event.metaKey) && !event.shiftKey) {
                var index = this.getNodeIndexInSelected(item);

                if (index === -1)
                    this.selected.push(item);

                if (index !== -1)
                    this.selected = this.selected.filter(x => x.key != item.key);
            }
            else if (event.shiftKey) {
                var arr = this.tableEl.value as Array<go.ObjectData>;
                var from = elIndex;
                var to = this.lastSelectedIndex;
                if (from > to) {
                    var temp = from;
                    from = to;
                    to = temp;
                }
                arr.forEach((item, index) => {
                    if (index >= from && index <= to) {
                        this.selected.push(item);
                    }
                });

            }
            else {
                if (!isCheckboxClicked)
                    this.selected = [];

                var index = this.getNodeIndexInSelected(item);

                if (index === -1)
                    this.selected.push(item);

                if (isCheckboxClicked && index !== -1) {
                    this.selected = this.selected.filter(x => x.key != item.key);
                }

            }

            if (this.selected && this.selected.length > 0) {
                var sel = this.selected[0];
                var part = this.diagram.findPartForKey(sel.key);
                if (part) {
                    this.diagram.centerRect(part.actualBounds);
                }
            }

        }

        var selectedParts: go.Part[] = [];

        this.selected.forEach(d => {
            selectedParts.push(this.getPartByKey(d.key));
        })
        this.diagram.selectCollection(selectedParts);
        this.lastSelectedIndex = elIndex;
    }

    private getNodeIndexInSelected(data: go.ObjectData) {
        return this.selected.indexOf(data);
    }

    private getTableCurrentData(): go.ObjectData[] {
        if (this.tableEl && this.tableEl['filteredValue']) {
            return this.tableEl['filteredValue'] as go.ObjectData[];
        }
        return this.nodeArray;

    }

    public nodeSelectedTrigger(data: go.ObjectData) {
        if (data) {
            var rows = this.tableEl['_rows'];
            var index = this.getTableCurrentData().indexOf(data) + 1;
            var page = Math.ceil(index / rows);
            this.tableEl['_first'] = (page - 1) * rows;
        }
        else {
            this.tableEl['_first'] = 0;
        }
        this.cdRef.markForCheck();
    }


    public clearSearchValue() {
        if (this.searchValue) {
            this.searchValue = '';
            this.tableEl.filterGlobal(this.searchValue, 'contains')
        }
    }

}