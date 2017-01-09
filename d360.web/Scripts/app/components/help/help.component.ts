import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-help-component',
    template: `
        <div class="row">
            <div class="col s10 offset-s1">
                <div class="tile tile-detail">
                    <header>
                        Tutorials
                    </header>

                    <div class="row">
                        <div class="col s12 m4 l4">
                            <h4>Security Overview</h4>
                            <div class="directions">In this session we will see how to create users, groups, and responsibility types.</div>
                        </div>
                        <div class="col s12 m8 l8">
                            <iframe src="//fast.wistia.net/embed/playlists/n5dmlh1fmk?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=324&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" name="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" height="324"></iframe>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col s12 m4 l4">
                            <h4>Metamodel Overview</h4>
                            <div class="directions">In this session we will walk through how to create the various types of assets in the Data3Sixty metamodel, including artifact types, model types, and attribute types.</div>
                        </div>
                        <div class="col s12 m8 l8">
                            <iframe src="//fast.wistia.net/embed/playlists/yvgr80adhn?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=316&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" name="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" height="316"></iframe>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col s12 m4 l4">
                            <h4>Relationships Overview</h4>
                            <div class="directions">In this session we will work with relationships types, explaining how they connect all your Data3Sixty assets together.</div>
                        </div>
                        <div class="col s12 m8 l8">
                            <iframe src="//fast.wistia.net/embed/playlists/2k5gywnx3m?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=316&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" name="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" height="316"></iframe>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col s12 m4 l4">
                            <h4>Integration</h4>
                            <div class="directions">This session covers bulk loading data into the system.</div>
                        </div>
                        <div class="col s12 m8 l8">
                            <iframe src="//fast.wistia.net/embed/playlists/jz5e0l1ep9?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=324&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" name="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" height="324"></iframe>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col s12 m4 l4">
                            <h4>Workflow</h4>
                            <div class="directions">This session covers high-level workflow concepts within the Data3Sixty system.</div>
                        </div>
                        <div class="col s12 m8 l8">
                            <iframe src="//fast.wistia.net/embed/playlists/bs2wblakyv?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=360&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" name="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" height="360"></iframe>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col s12 m4 l4">
                            <h4>Metrics</h4>
                            <div class="directions">These sessions give an overview of the options for creating dashboards, reports, and analytics within Data3Sixty.</div>
                        </div>
                        <div class="col s12 m8 l8">
                            <iframe src="//fast.wistia.net/embed/playlists/tqayidfa9t?media_0_0%5BautoPlay%5D=false&media_0_0%5BcontrolsVisibleOnLoad%5D=false&theme=bento&version=v1&videoOptions%5BautoPlay%5D=true&videoOptions%5BplayerColor%5D=51a6dc&videoOptions%5BvideoHeight%5D=360&videoOptions%5BvideoWidth%5D=640&videoOptions%5BvolumeControl%5D=true" allowtransparency="true" frameborder="0" scrolling="no" class="wistia_playlist" name="wistia_playlist" allowfullscreen mozallowfullscreen webkitallowfullscreen oallowfullscreen msallowfullscreen width="100%" height="360"></iframe>
                        </div>
                    </div>

                </div>                
            </div>
        </div>
         `,
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class HelpComponent extends BaseComponent implements OnInit {
    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Help');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Help'));
    }
};