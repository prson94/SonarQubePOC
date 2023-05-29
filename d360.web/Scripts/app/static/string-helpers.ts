import { Operator, OperatorString } from "../models/operator.model";

export class StringHelpers {

	static isNullOrEmpty(value: string): boolean {
		return (value == null || value === '');
	}

	static formatAsPathString(value: string, replaceWithAngle: boolean = true): string {
		let replacement = (value !== '' && value !== null ? value : "");
		if (replaceWithAngle) {
			replacement = replacement.split(" > ").join("#pathSegmentDelimiter");
		}
		return replacement.split("<").join("&lt;").split(">").join("&gt;").split("#pathSegmentDelimiter").join(" <i class='fa fa-angle-right'></i> ");
	}

	static trimChar(string, charToRemove): string {
		while (string.charAt(0) == charToRemove) {
			string = string.substring(1);
		}

		while (string.charAt(string.length - 1) == charToRemove) {
			string = string.substring(0, string.length - 1);
		}

		return string;
	}

	static getOperatorFromString(op: string, val: string): OperatorString {
		if (!op) {
			return OperatorString.Equals;
		}
		const combination = op + " " + val;

		switch (combination) {
			case "eq true":
				return OperatorString.IsTrue;
			case "eq false":
				return OperatorString.IsFalse;
			case "ne null":
				return OperatorString.Populated;
			case "eq null":
				return OperatorString.Populated;
		}

		switch (op) {
			case "eq":
				return OperatorString.Equals;
			case "ct":
				return OperatorString.Contains;
			case "nct":
				return OperatorString.NotContains;
			case "eq":
				return OperatorString.Equals;
			case "ne":
				return OperatorString.NotEquals;
			case "lt":
				return OperatorString.LessThan;
			case "le":
				return OperatorString.LessThanOrEquals;
			case "gt":
				return OperatorString.GreaterThan;
			case "ge":
				return OperatorString.GreaterThanOrEquals;
			default:
				return OperatorString.Equals;
		}
	}
}