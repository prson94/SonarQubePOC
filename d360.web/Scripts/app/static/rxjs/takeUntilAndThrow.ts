import { MonoTypeOperatorFunction, Observable } from "rxjs";

/**
 * It's rxjs/takeUntil that doesn't wants to be notifier to be triggered
 * If notifier is triggered, we throw an error
 */
export function takeUntilAndThrow<T>(notifier: Observable<any>, getError: () => any): MonoTypeOperatorFunction<T> {
    return (source: Observable<T>) => {
        return new Observable<T>(subscriber => {
            const sourceSub = source.subscribe(subscriber);
            const notifierSub = notifier.subscribe(v => {
                const error = getError();
                subscriber.error(error);
                sourceSub.unsubscribe();
            });

            return {
                unsubscribe: () => {
                    sourceSub.unsubscribe();
                    notifierSub.unsubscribe();
                }
            };
        });
    };
}