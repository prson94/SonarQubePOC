import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';


@Component({
    selector: 'gallery-message-box',
    templateUrl: './gallery.message-box.component.html',
    styles: [
        `
        .gallery-section {
            padding: 0 16px 32px 16px;
        }

        .gallery-section h4 {
            padding-bottom: 8px;
        }
        `
    ],    changeDetection: ChangeDetectionStrategy.OnPush
})

export class GalleryMessageBoxComponent implements OnInit {
    properties: Array<any>;
    sampleUsage: string = '<ig-message-box [message]="message"></ig-message-box>';
    message: string = "Create one or more measures to complete your score definition";
    lorem: string = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. In rhoncus, nunc et sodales facilisis, lorem lacus auctor risus, id rhoncus eros nulla a libero. Integer dui metus, molestie vel congue nec, molestie a nibh. Nunc vel enim et tortor viverra fringilla vel sit amet nisi. Ut eu arcu a purus pretium consequat. Duis ultricies, justo ut laoreet rhoncus, sapien nibh blandit justo, sit amet vehicula ligula diam vitae ex. Praesent felis lectus, tincidunt varius tincidunt in, posuere eu turpis. Quisque vel laoreet tellus, et rhoncus mi. Integer ullamcorper orci velit, et bibendum dolor efficitur eu. Curabitur maximus est eu dui pharetra sagittis at sed magna.";

    ngOnInit(): void {
        this.properties = new Array();
        this.properties.push({ Name: "content", Type: "string", Description: "Text content to be displayed in the message box.", Default: "" });
        this.properties.push({ Name: "messagetype", Type: "string", Description: "String value for the type of message.[information, warning] are the options.", Default: "information" });
    }

   
}
