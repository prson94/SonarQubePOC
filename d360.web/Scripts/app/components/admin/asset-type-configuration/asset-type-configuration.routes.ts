import { Injectable } from '@angular/core';
import { Routes, ActivatedRouteSnapshot, RouterStateSnapshot, CanActivate } from '@angular/router';

import { ConfigurationAssetTypeListPageComponent } from './list/configuration-asset-type-list-page.component';
import { StubComponent } from './stub.compnoent';
import { AssetTypeClass } from '../../../models/asset.model';
import { ConfigurationAssetTypeEditorPageComponent } from './edit/configuration-asset-type-editor-page.component';
import { ConfigurationAssetTypeDeletePageComponent } from './delete/configuration-asset-type-delete-page.component';
import { ConfigurationAssetTypeFieldsPageComponent } from './tabs/fields/configuration-asset-type-fields-page.component';
import { ConfigurationAssetTypeOwnersPageComponent } from './tabs/owners/configuration-asset-type-owners-page.component';


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

@Injectable({ providedIn: 'root' })
class WhenCanSeeOwnersGuard extends CanActivateOnlyForAvailableTypeClasses {
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
        component: ConfigurationAssetTypeDeletePageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
    {
        path: ':typeClass/:uid/fields',
        component: ConfigurationAssetTypeFieldsPageComponent,
        canActivate: [WhenCanSeeFieldDefinitionsGuard]
    },
    {
        path: ':typeClass/:uid/owners',
        component: ConfigurationAssetTypeOwnersPageComponent,
        canActivate: [WhenCanSeeOwnersGuard]
    },
    {
        path: ':typeClass/:uid/allocations',
        component: StubComponent,
        canActivate: []
    },
    {
        path: ':typeClass/:uid/relationships',
        component: StubComponent,
        canActivate: []
    },
    {
        path: ':typeClass/:uid/log',
        component: StubComponent,
        canActivate: []
    },
    {
        path: ':typeClass',
        component: ConfigurationAssetTypeListPageComponent,
        canActivate: [WhenCanAccessBasicFeaturesGuard]
    },
];