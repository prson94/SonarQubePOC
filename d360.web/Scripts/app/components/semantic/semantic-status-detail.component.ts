import { ChangeDetectorRef, Component, EventEmitter, OnChanges, OnInit, Output, ViewChild } from '@angular/core';
import { DataProfileService } from '../../services/dataprofile.service';

@Component({
    selector: 'semantic-status-detail',
    templateUrl: './semantic-status-detail.component.html',
    styleUrls: ["semanticTypes.less"],
    providers: [DataProfileService],
    host: {
        "(document:click)": "clickedOutside($event)",
    }, 
})


export class SemanticStatusDetailComponent implements OnInit {
    @Output() close = new EventEmitter();
    statuses: any;
    isExportInProgress: boolean = false;
    isLoading = false;

    constructor(private dataProfileService: DataProfileService) { }

    ngOnInit() {
        this.isLoading = true;
        this.dataProfileService.getSemanticLookupList("statuses").subscribe((res) => {
            this.statuses = res;
            this.isLoading = false;
        });        
    }

    export() {
        this.isExportInProgress = true;
        this.dataProfileService.getSemanticLookupList("statuses", true, () => { this.isExportInProgress = false });        
    }

    clickedOutside(event: any) {
        if (!(event.path.filter((f) => f?.classList?.contains("secondary-side-panel")).length > 0)) {
            this.close.emit();
        }
    }
}