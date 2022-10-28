import { Injectable } from '@angular/core';
import { Routes, ActivatedRouteSnapshot, RouterStateSnapshot, CanActivate } from '@angular/router';

import { ConfigurationAssetTypeListPageComponent } from './list/configuration-asset-type-list-page.component';
import { StubComponent } from './stub.compnoent';
import { AssetTypeClass } from '../../../models/asset.model';
import { ConfigurationAssetTypeEditorPageComponent } from './edit/configuration-asset-type-editor-page.component';


abstract class CanActivateOnlyForAvailableTypeClasses implements CanActivate {
    protected abstract typeClasses: AssetTypeClass[];
    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
        return this.typeClasses.includes(AssetTypeClass[route.params.typeClass as string]);
    }
}

@Injectable({ providedIn: 'root' })
class WhenCanAccessBasicFeaturesGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
        AssetTypeClass.TechnicalAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanCreateNewAssetTypeChildGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
        AssetTypeClass.TechnicalAsset
    ]
}

@Injectable({ providedIn: 'root' })
class WhenCanSeeFieldDefinitionsGuard extends CanActivateOnlyForAvailableTypeClasses {
    protected typeClasses: AssetTypeClass[] = [
        AssetTypeClass.BusinessAsset,
        AssetTypeClass.TechnicalAsset
    ]
}

export const assetTypeConfigurationRoutes: Routes = [
    {
        path: ':typeClass/new',
        component: ConfigurationAssetTypeEditorPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:parentUid/new',
        component: ConfigurationAssetTypeEditorPageComponent,
        canActivate: [WhenCanCreateNewAssetTypeChildGuard]
    },
    {
        path: ':typeClass/:uid/edit',
        component: ConfigurationAssetTypeEditorPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:uid/delete',
        component: StubComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:uid/fields',
        component: StubComponent,
        canActivate: [WhenCanSeeFieldDefinitionsGuard]
    },
    {
        path: ':typeClass',
        component: ConfigurationAssetTypeListPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
];