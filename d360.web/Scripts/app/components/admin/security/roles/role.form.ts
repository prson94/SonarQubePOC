import { Component, EventEmitter, Input, Output, SimpleChange } from '@angular/core';
import * as DOMPurify from 'dompurify';
import { SecurityService } from '../../../../services/security.service';
import { ReadRole } from '../../../../models/security.model';

@Component({
    selector: 'admin-roles-form',
    templateUrl: './role.form.html',
    providers: [SecurityService],
})

export class RoleForm {
	@Input() item: ReadRole;
    @Output() onSaveComplete = new EventEmitter();
    @Output() onLoadComplete = new EventEmitter();
    @Output() onCancel = new EventEmitter();

    isLoading = true;

    saveLabel = $localize`Save`;
    cancelLabel = $localize`Cancel`;

	constructor(private securityService: SecurityService) {

    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        let reloadRequired = false;
        for (const p in changes) {
            if (p === 'id') {
                if (changes[p].currentValue === 0) {
                    this.load();
                }
                if (changes[p].currentValue && (changes[p].currentValue !== changes[p].previousValue)) {
                    reloadRequired = true;
                }
                if (reloadRequired)
                    {this.load();}
            }
        }
    }

    load(): void {
		if (this.item) {
			if (this.item.description) {
				this.item.description = DOMPurify.sanitize(this.item.description);
			}
		}
		else {
			this.item = new ReadRole();
		}
    }

    save(): void {
        this.isLoading = true;

        if (this.item.uid !== "") {
			this.securityService.createRole(this.item)
                .subscribe((d) => {
                    this.isLoading = false;
                    this.onSaveComplete.emit(d);
                });
        } else {
			this.securityService.updateRole(this.item)
                .subscribe((d) => {
                    this.isLoading = false;
                    this.onSaveComplete.emit(d);
                });
        }
    }

    cancel(): void {
        this.onCancel.emit(null);
    }

    private isValid() {
        let valid = true;

		if (!this.item.name || this.item.name.length <= 0 || this.item.name.length > 250) {
            valid = false;
        }
        if (this.item.description && this.item.description.length > 4000) {
            valid = false;
        }

		return valid;
    }
}
