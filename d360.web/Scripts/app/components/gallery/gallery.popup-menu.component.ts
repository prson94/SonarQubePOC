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
            title: 'Edit',
            icon: 'fa-pencil'
        },
        {
            title: 'New',
            icon: 'fa-plus'
        },
        {
            title: 'Delete',
            icon: 'fa-thrash'
        },
        {
            isSeparator: true
        },
        {
            title: 'Exit'
        }
    ]


    private multiExample = [
        {
            title: 'Operators',
            icon: 'fa-pencil',
            items: [{
                title: 'No Edit',
                icon: 'fa-plus'
            },
            {
                title: 'New',
                icon: 'fa-minus'
            },
            {
                title: 'Delete',
                icon: 'fa-times'
            },
            {
                isSeparator: true
            },
            {
                title: 'No operator'
            }]
        },
        {
            title: 'New',
            icon: 'fa-plus',
            items: [{
                title: 'New nothins',
                disabled: true
            },
            {
                title: 'New new',
                items: [{
                    title: 'Yes this works too'
                },
                {
                    title: '2nd works here too'
                },
                {
                    title: 'Try me',
                    items: [{
                        title: 'Last one'
                    }]
                }
                ]
            },
            {
                title: 'Third item'
            }
            ]
        },
        {
            title: 'Delete',
            icon: 'fa-thrash'
        },
        {
            isSeparator: true
        },
        {
            title: 'Exit'
        }
    ]

    private tooltipExample = [
        {
            title: 'Edit',
            icon: 'fa-pencil'
        },
        {
            title: 'New',
            icon: 'fa-plus'
        },
        {
            title: 'Delete',
            icon: 'fa-thrash'
        },
        {
            isSeparator: true
        },
        {
            title: 'Child has tooltip',
            items: [{
                title: 'Hover over me',
                tooltip: 'I am tooltip and I am positioned above element. I am cool.'
            }]
        }
    ]

    private defaultExample = [
        {
            title: 'Edit',
            icon: 'fa-pencil'
        },
        {
            title: 'New',
            icon: 'fa-plus',
            default: true
        },
        {
            title: 'Delete',
            icon: 'fa-thrash'
        },
        {
            isSeparator: true
        },
        {
            title: 'Exit'
        }
    ]

    private labelExample = [
        {
            title: 'Options:',
            isLabel: true
        },
        {
            title: 'Edit',
            icon: 'fa-pencil'
        },
        {
            title: 'New',
            icon: 'fa-plus',
            default: true
        },
        {
            title: 'Delete',
            icon: 'fa-thrash',
            items: [
                {
                    title: 'Mandy Bank <br/> mbank@infogix.com',
                    isLabel: true
                },
                {
                    title: 'Edit',
                    icon: 'fa-pencil'
                },
                {
                    title: 'New',
                    icon: 'fa-plus',
                    default: true
                },
                {
                    title: 'Delete',
                    icon: 'fa-thrash'
                },
                {
                    isSeparator: true
                },
                {
                    title: 'Exit'
                }
            ]
        },
        {
            isSeparator: true
        },
        {
            title: 'Exit'
        }
    ]

    private checkExample = [
        {
            title: 'New'
        },
        {
            isSeparator: true
        }
        ,
        {
            title: 'Edit'
        },
        {
            title: 'Duplicate'
        },
        {
            title: 'Delete',
            disabled: true
        },
        {
            isSeparator: true
        },
        {
            title: 'Show Optional Fields',
            hasCheckbox: true,
            isChecked: true
        },
        {
            title: 'Show Beta Features',
            hasCheckbox: true
        }
    ]

    private keyboardShortcuts = [
        {
            title: 'Actions',
            isLabel: true
        },
        {
            title: 'Copy',
            keys: [17, 67]
        },
        {
            title: 'Paste',
            keys: [17, 86]
        },
        {
            title: 'Cut',
            keys: [17, 88]
        },
        {
            title: 'Delete',
            keys: [46]
        },
        {
            isSeparator: true
        },
        {
            title: 'Show Optional Fields',
            hasCheckbox: true,
            isChecked: true
        },
        {
            title: 'Show Beta Features',
            hasCheckbox: true
        }
    ]


    private badgeExample = [
        {
            title: 'Edit',
            icon: 'fa-pencil'
        },
        {
            title: 'New',
            icon: 'fa-plus',
            badge: {
                text: 'Im text badge',
                variant: 'negative'
            }
        },
        {
            title: 'Delete',
            icon: 'fa-thrash',
            badge: {
                text: '23',
                variant: 'default'
            }
        },
        {
            isSeparator: true
        },
        {
            title: 'Exit'
        }
    ]
}
