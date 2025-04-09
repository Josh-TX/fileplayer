import type { CopyItemsRequest, DeleteItemsRequest, DirContentsResponse, GetDurationsResponse, MediaInfo, Settings, UploadFromUrlRequest, UrlInfo } from "@/models/models";
const baseUrl = import.meta.env.VITE_BASE_URL;
import { toastrService } from "./ToastrService";

class ApiAccess {

    async getDirContents(path: string): Promise<DirContentsResponse | null> {
        const response = await fetch(`${baseUrl}/api/dir-contents?path=${encodeURIComponent(path)}`);
        if (!response.ok){
            var errorMessage = await response.text();
            if (response.status == 400 && errorMessage == "directory was a file"){
                return null;
            }
            errorMessage = errorMessage.replace(/^"|"$/g, '');
            var displayedMessage = errorMessage.length < 500 ? errorMessage : "An error occurred. See console for details";
            toastrService.showError(displayedMessage);
        }
        var json = await response.json();
        return <DirContentsResponse>json
    }
    async searchDirContents(path: string, filter: string, considerFolders: boolean, typoTolerance: boolean, anyOrder: boolean, regex: boolean): Promise<DirContentsResponse> {
        var url = `${baseUrl}/api/dir-contents?path=${encodeURIComponent(path)}&filter=${filter}`;
        if (considerFolders) url += "&considerFolderContents=true";
        if (typoTolerance) url += "&typoTolerance=true";
        if (anyOrder) url += "&anyOrder=true";
        if (regex) url += "&regex=true";
        var json = await this.fetchAndLog(url).then(res => res.json());
        return <DirContentsResponse>json
    }

    async getDurations(path: string): Promise<GetDurationsResponse> {
        var json = await this.fetchAndLog(`${baseUrl}/api/dir-contents/durations?path=${encodeURIComponent(path)}`).then(res => res.json());
        return <GetDurationsResponse>json
    }

    async getMediaInfo(path: string): Promise<MediaInfo> {
        var json = await this.fetchAndLog(`${baseUrl}/api/media-info?path=${encodeURIComponent(path)}`).then(res => res.json());
        return <MediaInfo>json
    }

    async updateProgress(progress: number, path: string): Promise<any> {
        await this.fetchAndLog(`${baseUrl}/api/update-progress?progress=${progress}&path=${encodeURIComponent(path)}`, {method: "POST"});
        return;
    }

    async getSettings(): Promise<Settings> {
        return await this.fetchAndLog(`${baseUrl}/api/settings`).then(res => res.json());
    }

    async updateSettings(settings: Settings): Promise<any> {
        await this.fetchAndLog(`${baseUrl}/api/update-settings`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(settings)
        });
    }

    async uploadFiles(file: File, path: string): Promise<any> {
        const formData = new FormData();
        formData.append('file', file);
        await this.fetchAndLog(`${baseUrl}/api/upload-files?path=${encodeURIComponent(path)}`, {
            method: "POST", 
            body: formData
        });
    }

    async createDir(path: string): Promise<any> {
        return await this.fetchAndLog(`${baseUrl}/api/create-dir?path=${encodeURIComponent(path)}`, {method: "POST"});
    }

    async copyItems(request: CopyItemsRequest): Promise<any> {
        await this.fetchAndLog(`${baseUrl}/api/copy-items`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(request)
        });
    }

    async deleteItems(request: DeleteItemsRequest): Promise<any> {
        await this.fetchAndLog(`${baseUrl}/api/delete-items`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(request)
        });
    }

    async rename(path: string, newName: string): Promise<any> {
        return await this.fetchAndLog(`${baseUrl}/api/rename?path=${encodeURIComponent(path)}&newName=${encodeURIComponent(newName)}`, {method: "POST"});
    }

    async getUrlInfo(url: string): Promise<UrlInfo> {
        return await this.fetchAndLog(`${baseUrl}/api/get-url-info?url=${encodeURIComponent(url)}`).then(res => res.json());
    }

    async uploadFromUrl(request: UploadFromUrlRequest): Promise<any> {
        return await this.fetchAndLog(`${baseUrl}/api/upload-from-url`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(request)});
    }

    async fetchAndLog(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
        try {
            const response = await fetch(input, init);
    
            if (!response.ok) {
                var errorMessage = await response.text();
                errorMessage = errorMessage.replace(/^"|"$/g, '');
                var displayedMessage = errorMessage.length < 500 ? errorMessage : "An error occurred. See console for details";
                toastrService.showError(displayedMessage);
            }
            return response;
        } catch (error) {
            console.error("Network or other error during fetch:", (error as Error).message);
            throw error;
        }
    }
}

export var apiAccess = new ApiAccess()