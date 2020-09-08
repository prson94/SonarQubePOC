import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-loading',
    templateUrl: './gallery.loading.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryLoadingComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<d3s-loading isLoading="true"></d3s-loading>';
    protected isLoading1: boolean = true;
    protected isLoading2: boolean = false;

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "isLoading", Type: "boolean", Description: "Whether or not to show the loading wheel", Default: "" });
        this.properties.push({ Name: "showTransparentLoader", Type: "boolean", Description: "Show a transparent background behind the loading wheel", Default: "false" });
    }
}
