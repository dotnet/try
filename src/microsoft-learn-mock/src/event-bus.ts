const idProperty = '__event_type_id__';

let nextId = 0;

/**
 * Enables publish/subscribe style communication between components.
 */
export class EventBus {
    private callbacks: { [eventType: number]: ((event: any) => void)[] } = {};

    /**
     * Publish an event.
     * @param event The event to publish.
     */
    public publish<T>(event: T) {
        // find the id of this class of event.
        const id = ((<any>event).constructor as { [key: string]: any })[idProperty] as number;
        if (id === undefined || this.callbacks[id] === undefined) {
            return;
        }

        // grab the callbacks for this type of event.
        const callbacks = this.callbacks[id].slice(0); // shallow clone the array in case subscription is disposed while publishing.

        // publish the event to it's subscribers.
        for (const callback of callbacks) {
            callback(event);
        }
    }

    /**
     * Subscribe to an event.
     * @param eventType The type of event.
     * @param callback A function to be invoked when the event is fired.
     * @returns A function that can be used to dispose of the event subscription.
     */
    public subscribe<T>(
        eventType: new (...args: any[]) => T,
        callback: (event: T) => void
    ): () => void {
        // ensure this class of event has an id.
        if (!eventType.hasOwnProperty(idProperty)) {
            (eventType as { [key: string]: any })[idProperty] = nextId++;
        }

        // get the id for this class of event.
        const id = (eventType as { [key: string]: any })[idProperty] as number;

        // ensure this class of event has a callbacks array.
        if (this.callbacks[id] === undefined) {
            this.callbacks[id] = [];
        }

        // push the callback into the callbacks array for this class of event.
        const callbacks = this.callbacks[id];
        if (callbacks.indexOf(callback) === -1) {
            callbacks.push(callback);
        }

        // return a "dispose" function to enable unsubscribing.
        return () => this.unsubscribe(eventType, callback);
    }

    /**
     * Unsubscribe from an event.
     * @param eventType The type of event.
     * @param callback The function to be unsubscribed.
     */
    public unsubscribe<T>(eventType: new (...args: any[]) => T, callback: (event: T) => void) {
        // get the id for this class of event.
        const id = (eventType as { [key: string]: any })[idProperty] as number;

        if (id === undefined || this.callbacks[id] === undefined) {
            return;
        }

        // unsubscribe the function
        const callbacks = this.callbacks[id];
        const index = callbacks.indexOf(callback);
        if (index !== -1) {
            callbacks.splice(index, 1);
        }
    }

    public dispose() {
        this.callbacks = {};
    }
}

/**
 * The global event bus.
 */
export const eventBus = new EventBus();