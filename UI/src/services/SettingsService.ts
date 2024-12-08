import { apiAccess } from '@/services/ApiAccess';
import type { Settings } from "@/models/models";


class SettingsService {
    private _settings: Settings = {
        sortBy: "name",
        playbackSpeed: 1,
        sortDesc: true
    }
    private _settingsPromise: Promise<Settings>;

    constructor(){
        this._settingsPromise = apiAccess.getSettings().then(z => {
            this._settings = z;
            return z;
        })
    }

    getSettingsAsync(): Promise<Settings>{
        return this._settingsPromise;
    }

    updatePlaybackSpeed(speed: number){
        this._settings.playbackSpeed = speed;
        apiAccess.updateSettings(this._settings);
    }

    updateSort(sortBy: string, sortDesc: boolean){
        this._settings.sortBy = sortBy;
        this._settings.sortDesc = sortDesc;
        apiAccess.updateSettings(this._settings);
    }
}
export var settingsService = new SettingsService()

