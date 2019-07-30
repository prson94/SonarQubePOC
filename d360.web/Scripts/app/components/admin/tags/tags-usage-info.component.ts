import { Component, Input, Output, HostListener, EventEmitter, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { TagService } from '../../../services/tag.service';


@Component({
    selector: 'd3s-tag-usage',
    templateUrl: 'tags-usage-info.component.html',
    providers: [TagService]
})

export class TagUsageInfoBox  {
    @Input() uid: string = '';
    private tooltipHTML: string = `<i class="fa fa-spinner fa-spin fa-2x"></i>`;
    private isContentLoaded: boolean = false;

    constructor(private tagsService: TagService) {

    }

    loadContent() {
        if (!this.isContentLoaded) {
            this.tagsService.getAssetPathsForTag(this.uid).subscribe(assets => {
                let tableHTML: string = '';
                assets.forEach(a => {
                    tableHTML += `<tr><td class="name">${a.DisplayValue}</td><td class="suppressed">${a.AssetPath}</td></tr>`;
                })

                this.tooltipHTML = `<table class="table table-borderless">${tableHTML}</div>`;
                
                this.isContentLoaded = true;
            })

        }
    }
}

