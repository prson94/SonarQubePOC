import { Component, ChangeDetectionStrategy, Input } from "@angular/core";

interface ComponentProperty {
    Name: string;
    Type: string;
    Description: string;
    Default: string;
}

@Component({
    selector: "gallery-component-properties",
    templateUrl: "./gallery.component-properties.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryComponentPropertiesComponent {
    @Input() header: string;
    @Input() properties: ComponentProperty[];
}