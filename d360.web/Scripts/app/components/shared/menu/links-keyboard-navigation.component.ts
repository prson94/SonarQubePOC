import { Component } from '@angular/core';
import * as _ from 'lodash';



const upKeyCode = 38;
const downKeycode = 40;
const enterKeyCode = 13;

@Component({
    selector: 'd3s-links-keyboard-navigation',
    templateUrl: 'links-keyboard-navigation.component.html'
})
export class LinksKeyboardNavigationComponent {
    private currentButtonIndex: number = -1;

    checkKey(event, elem) {
        if (event.keyCode == downKeycode || event.keyCode == enterKeyCode || event.keyCode == upKeyCode) {

            let allAItems = elem.getElementsByTagName("a");
            if (!allAItems.length)
                return;

            if (event.keyCode == enterKeyCode) {
                const item = allAItems[this.currentButtonIndex];
                if (item) {
                    item.click();
                }
            }
            if (event.keyCode == downKeycode) {
                this.currentButtonIndex++;
            } else if (event.keyCode == upKeyCode) {
                this.currentButtonIndex--;
            }

            if (allAItems.length - 1 < this.currentButtonIndex || this.currentButtonIndex < 0)
                this.currentButtonIndex = 0;

            this.resetColor(allAItems);
            let arr = allAItems[this.currentButtonIndex].className.split(" ");
            if (arr.indexOf("highlight") == -1) {
                allAItems[this.currentButtonIndex].className += " highlight";
            }

        }
    }

    resetColor(allAItems) {
        if (allAItems.length) {
            Array.prototype.forEach.call(allAItems, function (item) {
                item.className = item.className.replace(/\b highlight\b/g, "");
            });
        }
    }
}