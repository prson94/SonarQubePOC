import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReferenceV2Component } from './reference-v2.component';
import { ReferenceItemTypeListV2Component } from './list/reference-item-type-list-v2.component';
import { ReferenceItemTypeDefinitionComponent } from './tabs/definition/reference-item-type-definition.component';
import { ReferenceItemTypeItemsComponent } from './tabs/items/referrence-item-type-items.component';
import { ReferenceItemTypeFieldsComponent } from './tabs/fields/reference-item-type-fields.component';
import { ReferenceItemTypeLogComponent } from './tabs/log/reference-item-type-log.component';
import { ReferenceItemTypeRelationshipsComponent } from './tabs/relationships/referemce-item-type-relationships.component';
import { ReferenceItemTypeResponsibilitiesComponent } from './tabs/responsibilities/reference-item-type-responsibilities.component';
import { ReferenceItemTypeAssignmentsComponent } from './tabs/assignments/reference-item-type-assignments.component';

const routes: Routes = [
	{
		path: '',
		component: ReferenceV2Component,
		children: [
			{ path: "", component: ReferenceItemTypeListV2Component },
			{ path: ":uid/details", component: ReferenceItemTypeDefinitionComponent },
			{ path: ":uid/items", component: ReferenceItemTypeItemsComponent },
			{ path: ":uid/fields", component: ReferenceItemTypeFieldsComponent },
			{ path: ":uid/log", component: ReferenceItemTypeLogComponent },
			{ path: ":uid/relationships", component: ReferenceItemTypeRelationshipsComponent },
			{ path: ":uid/owners", component: ReferenceItemTypeResponsibilitiesComponent },
			{ path: ":uid/assignments", component: ReferenceItemTypeAssignmentsComponent }
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule],
})
export class ReferenceV2RoutingModule { }