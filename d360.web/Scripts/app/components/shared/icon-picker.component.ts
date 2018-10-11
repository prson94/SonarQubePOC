import { Component, Input, Output, EventEmitter, NgModule, } from '@angular/core';
import { BaseComponent } from './base.component';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'd3s-icon-picker',
    template: `
                <select name="icon" [ngModel]="ngModel" (ngModelChange)="ngModel=$event;ngModelChange.emit(ngModel);" style="width:100%">
                    <optgroup label="Web Application Icons">
                        <option value="fa-adjust">adjust</option>
                        <option value="fa-asterisk">asterisk</option>
                        <option value="fa-ban">ban</option>
                        <option value="fa-bar-chart">bar-chart</option>
                        <option value="fa-barcode">barcode</option>                        
                        <option value="fa-beer">beer</option>
                        <option value="fa-bell">bell</option>
                        <option value="fa-bell-o">bell-alt</option>
                        <option value="fa-bolt">bolt</option>
                        <option value="fa-bomb">bomb</option>
                        <option value="fa-book">book</option>
                        <option value="fa-bookmark">bookmark</option>
                        <option value="fa-bookmark-o">bookmark-empty</option>
                        <option value="fa-briefcase">briefcase</option>
                        <option value="fa-bullhorn">bullhorn</option>
                        <option value="fa-bullseye">bullseye</option>
                        <option value="fa-bug">bug</option>
                        <option value="fa-bus">bus</option>
                        <option value="fa-cab">cab</option>
                        <option value="fa-calculator">calculator</option>
                        <option value="fa-calendar">calendar</option>
                        <option value="fa-camera">camera</option>
                        <option value="fa-camera-retro">camera-retro</option>
                        <option value="fa-car">car</option>
                        <option value="fa-certificate">certificate</option>
                        <option value="fa-chain">chain</option>
                        <option value="fa-check">check</option>                
                        <option value="fa-check-circle">check circle</option>
                        <option value="fa-circle">circle</option>
                        <option value="fa-circle-o">circle empty</option>
                        <option value="fa-cloud">cloud</option>
                        <option value="fa-cloud-download">cloud-download</option>
                        <option value="fa-cloud-upload">cloud-upload</option>
                        <option value="fa-code">code</option>
                        <option value="fa-coffee">coffee</option>
                        <option value="fa-cog">cog</option>
                        <option value="fa-cogs">cogs</option>
                        <option value="fa-comment">comment</option>
                        <option value="fa-comment-o">comment empty</option>
                        <option value="fa-comments">comments</option>
                        <option value="fa-comments-o">comments empty</option>
                        <option value="fa-credit-card">credit-card</option>
                        <option value="fa-credit-card-alt">credit-card-alt</option>
                        <option value="fa-cube">cube</option>
                        <option value="fa-cubes">cubes</option>
                        <option value="fa-dashboard">dashboard</option>
                        <option value="fa-database">database</option>
                        <option value="fa-desktop">desktop</option>
                        <option value="fa-dollar">dollar</option>
                        <option value="fa-download">download</option>                        
                        <option value="fa-edit">edit</option>
                        <option value="fa-envelope">envelope</option>
                        <option value="fa-envelope-o">envelope-alt</option>
                        <option value="fa-envelope-open">envelope-open</option>
                        <option value="fa-envelope-open-o">envelope-open-alt</option>
                        <option value="fa-exchange">exchange</option>
                        <option value="fa-exclamation">exclamation</option>
                        <option value="fa-exclamation-circle">exclamation-circle</option>
                        <option value="fa-exclamation-triangle">exclamation-triangle</option>
                        <option value="fa-external-link">external-link</option>
                        <option value="fa-eye">eye</option>
                        <option value="fa-eye-slash">eye-slash</option>                        
                        <option value="fa-fighter-jet">fighter-jet</option>
                        <option value="fa-film">film</option>
                        <option value="fa-filter">filter</option>
                        <option value="fa-fire">fire</option>
                        <option value="fa-flag">flag</option>
                        <option value="fa-flask">flask</option>
                        <option value="fa-folder">folder-close</option>
                        <option value="fa-folder-open">folder-open</option>
                        <option value="fa-folder-o">folder-close-alt</option>
                        <option value="fa-folder-open-o">folder-open-alt</option>                        
                        <option value="fa-gift">gift</option>
                        <option value="fa-glass">glass</option>
                        <option value="fa-globe">globe</option>
                        <option value="fa-group">group</option>
                        <option value="fa-hdd-o">hdd</option>
                        <option value="fa-headphones">headphones</option>
                        <option value="fa-heart">heart</option>
                        <option value="fa-heart-o">heart-empty</option>
                        <option value="fa-home">home</option>
                        <option value="fa-inbox">inbox</option>
                        <option value="fa-info">info</option>
                        <option value="fa-info-circle">info-circle</option>
                        <option value="fa-key">key</option>
                        <option value="fa-leaf">leaf</option>
                        <option value="fa-laptop">laptop</option>
                        <option value="fa-legal">legal</option>
                        <option value="fa-lemon-o">lemon</option>
                        <option value="fa-lightbulb-o">lightbulb</option>
                        <option value="fa-lock">lock</option>
                        <option value="fa-unlock">unlock</option>
                        <option value="fa-magic">magic</option>
                        <option value="fa-magnet">magnet</option>
                        <option value="fa-map-marker">map-marker</option>
                        <option value="fa-minus">minus</option>
                        <option value="fa-minus-circle">minus-circle</option>
                        <option value="fa-mobile-phone">mobile-phone</option>
                        <option value="fa-money">money</option>                        
                        <option value="fa-music">music</option>                        
                        <option value="fa-paw">paw</option>
                        <option value="fa-pencil">pencil</option>
                        <option value="fa-picture-o">picture</option>
                        <option value="fa-plane">plane</option>
                        <option value="fa-plus">plus</option>
                        <option value="fa-plus-circle">plus-circle</option>
                        <option value="fa-print">print</option>
                        <option value="fa-puzzle-piece">puzzle piece</option>
                        <option value="fa-qrcode">qrcode</option>
                        <option value="fa-question">question</option>
                        <option value="fa-quote-left">quote-left</option>
                        <option value="fa-quote-right">quote-right</option>
                        <option value="fa-random">random</option>
                        <option value="fa-recylcle">recycle</option>
                        <option value="fa-refresh">refresh</option>
                        <option value="fa-remove">remove</option>                        
                        <option value="fa-reorder">reorder</option>
                        <option value="fa-reply">reply</option>                                                
                        <option value="fa-road">road</option>
                        <option value="fa-rocket">rocket</option>
                        <option value="fa-rss">rss</option>                        
                        <option value="fa-search">search</option>
                        <option value="fa-share">share</option>
                        <option value="fa-share-alt">share-alt</option>
                        <option value="fa-shopping-cart">shopping-cart</option>
                        <option value="fa-signal">signal</option>
                        <option value="fa-sign-in">signin</option>
                        <option value="fa-sign-out">signout</option>
                        <option value="fa-sitemap">sitemap</option>
                        <option value="fa-sort">sort</option>
                        <option value="fa-sort-down">sort-down</option>
                        <option value="fa-sort-up">sort-up</option>
                        <option value="fa-spinner">spinner</option>
                        <option value="fa-star">star</option>
                        <option value="fa-star-o">star-empty</option>
                        <option value="fa-star-half">star-half</option>
                        <option value="fa-tablet">tablet</option>
                        <option value="fa-tag">tag</option>
                        <option value="fa-tags">tags</option>
                        <option value="fa-tasks">tasks</option>
                        <option value="fa-thumbs-down">thumbs-down</option>
                        <option value="fa-thumbs-up">thumbs-up</option>
                        <option value="fa-time">time</option>
                        <option value="fa-tint">tint</option>
                        <option value="fa-train">train</option>
                        <option value="fa-trash">trash</option>                        
                        <option value="fa-trophy">trophy</option>
                        <option value="fa-truck">truck</option>
                        <option value="fa-umbrella">umbrella</option>
                        <option value="fa-upload">upload</option>                        
                        <option value="fa-user">user</option>
                        <option value="fa-user-md">user-md</option>
                        <option value="fa-volume-off">volume-off</option>
                        <option value="fa-volume-down">volume-down</option>
                        <option value="fa-volume-up">volume-up</option>
                        <option value="fa-warning">warning</option>
                        <option value="fa-wrench">wrench</option>                        
                </optgroup>
                <optgroup label="Text Editor Icons">
                        <option value="fa-file">file</option>
                        <option value="fa-file-o">file-alt</option>
                        <option value="fa-cut">cut</option>
                        <option value="fa-copy">copy</option>
                        <option value="fa-paste">paste</option>
                        <option value="fa-save">save</option>
                        <option value="fa-undo">undo</option>
                        <option value="fa-repeat">repeat</option>
                        <option value="fa-text-height">text-height</option>
                        <option value="fa-text-width">text-width</option>
                        <option value="fa-align-left">align-left</option>
                        <option value="fa-align-center">align-center</option>
                        <option value="fa-align-right">align-right</option>
                        <option value="fa-align-justify">align-justify</option>
                        <option value="fa-indent">indent</option>                        
                        <option value="fa-font">font</option>
                        <option value="fa-bold">bold</option>
                        <option value="fa-italic">italic</option>
                        <option value="fa-strikethrough">strikethrough</option>
                        <option value="fa-underline">underline</option>
                        <option value="fa-link">link</option>
                        <option value="fa-paperclip">paperclip</option>
                        <option value="fa-columns">columns</option>
                        <option value="fa-table">table</option>
                        <option value="fa-th-large">th-large</option>
                        <option value="fa-th">th</option>
                        <option value="fa-th-list">th-list</option>
                        <option value="fa-list">list</option>
                        <option value="fa-list-ol">list-ol</option>
                        <option value="fa-list-ul">list-ul</option>
                        <option value="fa-list-alt">list-alt</option>
                </optgroup>
                <optgroup label="Directional Icons">
                        <option value="fa-angle-left">angle-left</option>
                        <option value="fa-angle-right">angle-right</option>
                        <option value="fa-angle-up">angle-up</option>
                        <option value="fa-angle-down">angle-down</option>
                        <option value="fa-arrow-down">arrow-down</option>
                        <option value="fa-arrow-left">arrow-left</option>
                        <option value="fa-arrow-right">arrow-right</option>
                        <option value="fa-arrow-up">arrow-up</option>
                        <option value="fa-caret-down">caret-down</option>
                        <option value="fa-caret-left">caret-left</option>
                        <option value="fa-caret-right">caret-right</option>
                        <option value="fa-caret-up">caret-up</option>
                        <option value="fa-chevron-down">chevron-down</option>
                        <option value="fa-chevron-left">chevron-left</option>
                        <option value="fa-chevron-right">chevron-right</option>
                        <option value="fa-chevron-up">chevron-up</option>
                        <option value="fa-arrow-circle-down">arrow-circle-down</option>
                        <option value="fa-arrow-circle-left">arrow-circle-left</option>
                        <option value="fa-arrow-circle-right">arrow-circle-right</option>
                        <option value="fa-arrow-circle-up">arrow-circle-up</option>
                        <option value="fa-angle-double-left">angle-double-left</option>
                        <option value="fa-angle-double-right">angle-double-right</option>
                        <option value="fa-angle-double-up">angle-double-up</option>
                        <option value="fa-angle-double-down">angle-double-down</option>
                        <option value="fa-hand-o-down">hand-down</option>
                        <option value="fa-hand-o-left">hand-left</option>
                        <option value="fa-hand-o-right">hand-right</option>
                        <option value="fa-hand-o-up">hand-up</option>
                        <option value="fa-circle">circle</option>
                        <option value="fa-circle-o">circle-blank</option>
              </optgroup>
              <optgroup label="Video Player Icons">
                        <option value="fa-play-circle">play-circle</option>
                        <option value="fa-play">play</option>
                        <option value="fa-pause">pause</option>
                        <option value="fa-stop">stop</option>
                        <option value="fa-step-backward">step-backward</option>
                        <option value="fa-fast-backward">fast-backward</option>
                        <option value="fa-backward">backward</option>
                        <option value="fa-forward">forward</option>
                        <option value="fa-fast-forward">fast-forward</option>
                        <option value="fa-step-forward">step-forward</option>
                        <option value="fa-eject">eject</option>                                           
              </optgroup>            
            <optgroup label="Medical Icons">
                        <option value="fa-ambulance">ambulance</option>                                                                   
                        <option value="fa-heart-o">heart</option>
                        <option value="fa-heart">heart filled</option>
                        <option value="fa-heartbeat">heartbeat</option>
                        <option value="fa-hospital-o">hospital</option>
                        <option value="fa-h-square">hospital sign</option>
                        <option value="fa-medkit">medkit</option>
                        <option value="fa-plus-square">plus-square</option>
                        <option value="fa-stethoscope">stethoscope</option>
                        <option value="fa-user-md">user-md</option>
                        <option value="fa-wheelchair">wheelchair</option>
                        <option value="fa-wheelchair-alt">wheelchair alt</option>
            </optgroup>
        </select>
    `
})

export class IconPickerComponent extends BaseComponent {
    @Input() ngModel: string;
    @Output() ngModelChange = new EventEmitter();
}

@NgModule({
    declarations: [
        IconPickerComponent
    ],
    exports: [
        IconPickerComponent
    ]
    , imports: [
        CommonModule,
        FormsModule,
    ],
    providers: []
})
export class IconPickerModule { }