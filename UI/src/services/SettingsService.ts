import { apiAccess } from '@/services/ApiAccess';
import type { Settings } from "@/models/models";


class SettingsService {
    private _settings: Settings = {
        sortBy: "name",
        playbackSpeed: 1,
        sortDesc: true,
        preferredHeight: 720,
        useMDate: false,
        compatCodec: false,
        useNative: false
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
        if (this._settings.playbackSpeed != speed){
            this._settings.playbackSpeed = speed;
            apiAccess.updateSettings(this._settings);
        }
    }

    updateSort(sortBy: string, sortDesc: boolean){
        if (this._settings.sortBy != sortBy || this._settings.sortDesc != sortDesc){
            this._settings.sortBy = sortBy;
            this._settings.sortDesc = sortDesc;
            apiAccess.updateSettings(this._settings);
        }
    }

    updatePreferredHeight(preferredHeight: number | null){
        if (this._settings.preferredHeight != preferredHeight){
            this._settings.preferredHeight = preferredHeight;
            apiAccess.updateSettings(this._settings);
        }
    }

    updateUseMDate(useMDate: boolean){
        if (this._settings.useMDate != useMDate){            
            this._settings.useMDate = useMDate;
            apiAccess.updateSettings(this._settings);
        }
    }

    updateCompatCodec(compatCodec: boolean){
        if (this._settings.compatCodec != compatCodec){            
            this._settings.compatCodec = compatCodec;
            apiAccess.updateSettings(this._settings);
        }
    }

    updateUseNative(useNative: boolean){
        if (this._settings.useNative != useNative){            
            this._settings.useNative = useNative;
            apiAccess.updateSettings(this._settings);
        }
    }
}
export var settingsService = new SettingsService()

