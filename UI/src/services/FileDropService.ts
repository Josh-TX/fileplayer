type callback = (files: File[]) => any

class FileDropService {
    _subscriptions: callback[] = []
    subscribe(callback: callback){
        this._subscriptions.push(callback)
    }
    unsubscribe(callback: callback){
        this._subscriptions = this._subscriptions.filter(z => z != callback);
    }
    filesDropped(files: File[]){
        for(var sub of this._subscriptions){
            sub(files);
        }
    }
}


export var fileDropService = new FileDropService();