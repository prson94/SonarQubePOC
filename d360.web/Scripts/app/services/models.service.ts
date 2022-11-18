import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { catchError, map } from "rxjs/operators";

import { HierarchyDiagramModel, Model, ModelHierarchy } from '../models/model.model';
import { JsonResult } from '../models/jsonresult.model';

import { MessagesObservableService } from './messages-observable.service';
import { BaseObservableService } from "./baseObservable.service";

@Injectable({
	providedIn: 'root'
})
export class ModelsService extends BaseObservableService {
	constructor(
		private http: HttpClient,
		messagesService: MessagesObservableService
	) {
		super(messagesService);
	}

	public getCatalogDiagram(uid: string): Observable<HierarchyDiagramModel[]> {
		return this
			.http
			.get(`diagrams/${uid}/InformationCatalogDiagramData`)
			.pipe(
				map((response) => <HierarchyDiagramModel[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	getModels(): Observable<Model[]> {
		return this.http.get('api/v2/assets/types?Class=Model')
			.pipe(
				map((response) => <Model[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	getModel(assetTypeUid: string): Observable<Model> {
		return this.http.get(`api/catalogs/${assetTypeUid}`)
			.pipe(
				map((response) => <Model>response),
				catchError((err) => this.handleError(err))
			);
	}

	getModelHierarchy(assetTypeUid: string): Observable<ModelHierarchy[]> {
		const url = `internal/taxonomy/ModelHierarchy?uid=${assetTypeUid}}`;

		return this.http.get(url)
			.pipe(
				map((response) => <ModelHierarchy[]>response),
				catchError((err) => this.handleError(err))
			);
	}

	saveModelHierarchy(hierarchy: ModelHierarchy): Observable<JsonResult> {
		if (hierarchy.ID == null || !hierarchy.ID) {
			return this.postDynamic(this.http, 'taxonomy', hierarchy);
		}

		return this.putDynamic(this.http, 'taxonomy', hierarchy);
	}
}
