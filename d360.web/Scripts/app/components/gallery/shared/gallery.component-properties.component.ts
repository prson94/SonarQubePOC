import { Component, ChangeDetectionStrategy, Input } from "@angular/core";


@Component({
    selector: "gallery-component-properties",
    templateUrl: "./gallery.component-properties.component.html",
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryComponentPropertiesComponent {
    @Input() header: string;
    @Input() properties: ComponentProperty[];
}

interface ComponentProperty {
    Name: string;
    Type: string;
    Description: string;
    Default: string;
}
