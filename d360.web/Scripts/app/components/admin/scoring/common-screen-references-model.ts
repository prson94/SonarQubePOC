import { MetricFieldTypeViewModel, MetricPathOptionViewModel } from "../../../models/metrics.model";
import { OperatorModel } from "../../../models/operator.model";
import { Predicate } from "../../../models/predicate.model";
import { RelationshipType } from "../../../models/relationship.model";
import { ResponsibilityType } from "../../../models/responsibility-type.model";

export class CommonScreenReferencesModel {
    fields: MetricFieldTypeViewModel[];
    operators: OperatorModel[];
    paths: MetricPathOptionViewModel[];
    predicates: Predicate[];
    relationships: RelationshipType[];
    responsibilities: ResponsibilityType[];

    constructor() {

    }
}