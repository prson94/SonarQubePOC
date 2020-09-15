import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-popup-menu',
    templateUrl: './gallery.popup-menu.component.html',
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

export class GalleryPopupMenuComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<ig-popup-menu></ig-popup-menu>';
    protected isLoading1: boolean = true;
    protected isLoading2: boolean = false;

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "isLoading", Type: "boolean", Description: "Whether or not to show the loading wheel", Default: "" });
        this.properties.push({ Name: "showTransparentLoader", Type: "boolean", Description: "Show a transparent background behind the loading wheel", Default: "false" });
    }

    private simpleExample = [
        {
            label: 'Edit',
            icon: 'fa-pencil'
        },
        {
            label: 'New',
            icon: 'fa-plus'
        },
        {
            label: 'Delete',
            icon: 'fa-thrash'
        },
        {
            isSeparator: true
        },
        {
            label: 'Exit'
        }
    ]


    private multiExample = [
        {
            label: 'Operators',
            icon: 'fa-pencil',
            items: [{
                label: 'No Edit',
                icon: 'fa-plus'
            },
            {
                label: 'New',
                icon: 'fa-minus'
            },
            {
                label: 'Delete',
                icon: 'fa-times'
            },
            {
                isSeparator: true
            },
            {
                label: 'No operator'
            }]
        },
        {
            label: 'New',
            icon: 'fa-plus',
            items: [{
                label: 'New nothins',
                disabled: true
            },
            {
                label: 'New new',
                items: [{
                    label: 'Yes this works too'
                },
                {
                    label: '2nd works here too'
                },
                {
                    label: 'Try me',
                    items: [{
                        label: 'Last one'
                    }]
                }
                ]
            }]
        },
        {
            label: 'Delete',
            icon: 'fa-thrash'
        },
        {
            isSeparator: true
        },
        {
            label: 'Exit'
        }
    ]
}
