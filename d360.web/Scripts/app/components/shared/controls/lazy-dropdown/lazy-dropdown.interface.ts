/**
 * Custom lazy load event.
 * @see {@link Dropdown.onLazyLoad}
 * @group Events
 */
export interface DropdownLazyLoadEvent {
	/**
	 * Index of the first element in viewport.
	 */
	first: number;
	/**
	 * Index of the last element in viewport.
	 */
	last: number;
	/**
	 * The current value of the filter.
	 */
	filter: string | null;
}