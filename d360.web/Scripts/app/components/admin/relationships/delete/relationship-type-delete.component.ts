import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { RelationshipType, RelationshipTypeSimpleUIModel } from '../../../../models/relationship.model';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { RelationshipsService } from '../../../../services/relationships.service';

@Component({
	selector: 'd3s-relationship-type-delete',
	templateUrl: './relationship-type-delete.component.html',
	styleUrls: ['./relationship-type-delete.component.less']
})
export class RelationshipTypeDeleteComponent implements OnChanges {
	@Input() relationshipTypeUid: string;
	@Input() isModalVisible: boolean;

	@Output() onCancel = new EventEmitter();
	@Output() onDelete = new EventEmitter();

	relationshipType: RelationshipTypeSimpleUIModel;
	deleteInProgress: boolean = false;

	constructor(
		private relationshipService: RelationshipsService,
		private messagesService: MessagesObservableService,
	) { }

	ngOnChanges(changes: SimpleChanges): void {
		if (changes && changes.relationshipTypeUid && changes.relationshipTypeUid.currentValue !== changes.relationshipTypeUid.previousValue) {
			this.load();
		}
	}

	async load() {
		if (!this.relationshipTypeUid) {
			return;
		}

		const result = await this.relationshipService.getRelationshipType(this.relationshipTypeUid).toPromise();
		if (result.length > 0) {
			this.relationshipType = result.map((rel) => RelationshipType.ConvertToUIModeldata(rel))[0];
		}
	}

	cancel() {
		this.onCancel.emit();
	}

	delete() {
		this.deleteInProgress = true;
		this.relationshipService.deleteRelationshipType(this.relationshipTypeUid)
			.subscribe((result) => {
				result = result[0];
				this.deleteInProgress = false;
				this.onDelete.emit(result);
				this.onCancel.emit();
			});

	}

}
