import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef, OnInit } from '@angular/core';
import { TagService } from '../../../services/tag.service';


@Component({
    selector: 'd3s-tag-usage',
    templateUrl: 'tags-usage-info.component.html',
    providers: [TagService]
})

export class TagUsageInfoBox implements OnInit {
    @Input() uid: string = '';
    private tooltipHTML: string = ``;
    private loadedUid: string = '';

    private isTooltipVisibe: boolean = false;
    private isMouseOnTooltip: boolean = false;
    constructor(private tagsService: TagService) {

    }

    ngOnInit() {
        if (this.uid) {
            this.load();
        }
    }

    load() {
        this.tooltipHTML = `<i class="fa fa-spinner fa-spin fa-2x"></i>`;
        this.tagsService.getAssetPathsForTag(this.uid).subscribe(assets => {
            let tableHTML: string = '';
            assets.forEach(a => {
                tableHTML += `<tr><td class="name"><a href='${a.Url}'>${a.DisplayName}</a></td><td class="suppressed">${a.Breadcrumbs}</td></tr>`;
            })

            this.tooltipHTML = `<table class="table table-borderless">${tableHTML}</div>`;
            this.loadedUid = this.uid;
        })
    }

    showContent(isFromTooltip) {
        if (this.uid != this.loadedUid)
            this.load();

        if (isFromTooltip)
            this.isMouseOnTooltip = true;

        this.isTooltipVisibe = true;


    }

    hideContent(isFromTooltip) {
        if (isFromTooltip)
            this.isMouseOnTooltip = false;

        if (!this.isMouseOnTooltip)
            this.isTooltipVisibe = false;
    }
}

