
class HistoryService {
    private _storageName = "fileplayer-events"
    private _events: StoredEvent[] = [];
    constructor(){
        var str = localStorage[this._storageName];
        this._events = str ? JSON.parse(str) : []

        //filter out events two weeks old
        var twoWeeksAgo = new Date();
        twoWeeksAgo.setDate(twoWeeksAgo.getDate() - 14);
        var twoWeeksAgoMs = twoWeeksAgo.getTime();
        this._events = this._events.filter(z => z.insertMs > twoWeeksAgoMs)
    }

    RecordEvent(e: Event){
        var nowMs = new Date().getTime();
        if (e.type == "playing" && this._events.length > 0 && this._events[0].type == "playing" && this._events[0].path == e.path){
            this._events[0].count++;
            this._events[0].updateMs = nowMs;
            this._events[0].currentTime = Math.max(this._events[0].currentTime, e.currentTime);
        } else {
            this._events.unshift({
                ...e,
                insertMs: nowMs,
                initialTime: e.currentTime,
                updateMs: nowMs,
                count: 1
            })
        }
        localStorage[this._storageName] = JSON.stringify(this._events);
    }

    GetEvents(): StoredEvent[]{
        return this._events.map(z => ({...z})).slice(0, 300);
    }
}
export var historyService = new HistoryService()

export type Event = {
    path: string,
    currentTime: number,
    type: EventType
}

export type EventType = "init" | "playing" | "error" | "seekforwards" | "seekbackwards" | "restarted" | "firstupdate" | "firstever"

type StoredEvent = Event & {
    insertMs: number,
    updateMs: number,
    initialTime?: number | undefined,
    count: number
}
