import { Component, OnInit, ChangeDetectionStrategy, HostListener } from '@angular/core';


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

        .fullscreen{
            position: fixed;
            background: #44444485;
            z-index: 15000;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
         }

        .fullscreen .message{
            background: white;
            width: 400px;
            text-align: center;
            margin: 0 auto;
            margin-top: 40px;
            padding: 20px;
            border-radius: 4px;
        }
        `
    ], changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryPopupMenuComponent implements OnInit {
    protected properties: Array<any>;
    protected sampleUsage: string = '<ig-popup-menu [items]="items"></ig-popup-menu>';
    protected isLoading1: boolean = true;
    protected isLoading2: boolean = false;

    cleanJsonExamples: any = {};
    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "items", Type: "Array<PopupMenuItem>", Description: "Array of menu items", Default: "Empty []" });
        this.properties.push({ Name: "items[i].title", Type: "string", Description: "Title of menu item", Default: "" });
        this.properties.push({ Name: "items[i].icon", Type: "string", Description: "Icon that will appear near title (font awesome class definition)", Default: "" });
        this.properties.push({ Name: "items[i].items", Type: "Array<PopupMenuItem>", Description: "Array of sub-menu items", Default: "Empty []" });
        this.properties.push({ Name: "items[i].disabled", Type: "boolean", Description: "If set to true item will be disabled and non-actionable", Default: "false" });
        this.properties.push({ Name: "items[i].tooltip", Type: "string", Description: "Tooltip that will appear over item when hovered", Default: "" });
        this.properties.push({ Name: "items[i].isLabel", Type: "boolean", Description: "If set to true title will be bolded and be non-actionable", Default: "false" });
        this.properties.push({ Name: "items[i].hasCheckbox", Type: "boolean", Description: "If set to true, checkmark will appear near menu item if isChecked value is true", Default: "false" });
        this.properties.push({ Name: "items[i].isChecked", Type: "boolean", Description: "If set to true, checkmark will be visible", Default: "false" });
        this.properties.push({ Name: "items[i].keys", Type: "Array<int>", Description: "Array of key codes that will trigger action (shortcut)", Default: "Empty []" });
        this.properties.push({ Name: "items[i].badge", Type: "Object<PopupMenuItemBadge>", Description: "Object that defines value and appearance of badge", Default: "null" });
        this.properties.push({ Name: "items[i].badge.text", Type: "string", Description: "Text that will appear in badge", Default: "" });
        this.properties.push({ Name: "items[i].badge.variant", Type: "string", Description: "String value for the style for the badge. [default, emphasis, positive, negative, warning and light] are the options", Default: "" });

        this.cleanJsonExamples['simpleExample'] = JSON.parse(JSON.stringify(this.simpleExample));
        this.cleanJsonExamples['multiExample'] = JSON.parse(JSON.stringify(this.multiExample));
        this.cleanJsonExamples['tooltipExample'] = JSON.parse(JSON.stringify(this.tooltipExample));
        this.cleanJsonExamples['defaultExample'] = JSON.parse(JSON.stringify(this.defaultExample));
        this.cleanJsonExamples['labelExample'] = JSON.parse(JSON.stringify(this.labelExample));
        this.cleanJsonExamples['checkExample'] = JSON.parse(JSON.stringify(this.checkExample));
        this.cleanJsonExamples['keyboardShortcuts'] = JSON.parse(JSON.stringify(this.keyboardShortcuts));
        this.cleanJsonExamples['badgeExample'] = JSON.parse(JSON.stringify(this.badgeExample));
    }

    simpleExample = [
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

    multiExample = [
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

    tooltipExample = [
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

    defaultExample = [
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

    labelExample = [
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

    checkExample = [
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

    keyboardShortcuts = [
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

    badgeExample = [
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

    showFull: boolean = false;
    @HostListener('document:keydown.escape', ['$event']) onKeydownHandler(event: KeyboardEvent) {
        this.showFull = false;
    }

    fullScreenExample = [
        {
            title: 'Options:',
            isLabel: true
        },
        {
            title: 'Edit',
            icon: 'fa-pencil',
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
            title: 'New',
            icon: 'fa-plus',
            default: true,
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
}
