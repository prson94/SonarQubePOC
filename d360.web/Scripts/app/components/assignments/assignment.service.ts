import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AssignmentService {

  setFormValidators: Subject<void> = new Subject<void>();
  loadAssignments:Subject<void>= new Subject<void>();
  getAssetDetailsSubject:Subject<void>= new Subject<void>();

  constructor() { }

}