import type { CopyItemsRequest, DeleteItemsRequest, DirContentsResponse, GetDurationsResponse, MediaInfo, Settings, UploadFromUrlRequest, UrlInfo } from "@/models/models";
const baseUrl = import.meta.env.VITE_BASE_URL;

class ApiAccess {

    async getDirContents(path: string): Promise<DirContentsResponse> {
        var json = await fetch(`${baseUrl}/api/dir-contents?path=${encodeURIComponent(path)}`).then(res => res.json());
        return <DirContentsResponse>json
    }
    async searchDirContents(path: string, filter: string, considerFolders: boolean, typoTolerance: boolean, anyOrder: boolean): Promise<DirContentsResponse> {
        var url = `${baseUrl}/api/dir-contents?path=${encodeURIComponent(path)}&filter=${filter}`;
        if (considerFolders) url += "&considerFolderContents=true";
        if (typoTolerance) url += "&typoTolerance=true";
        if (anyOrder) url += "&anyOrder=true";
        var json = await fetch(url).then(res => res.json());
        return <DirContentsResponse>json
    }

    async getDurations(path: string): Promise<GetDurationsResponse> {
        var json = await fetch(`${baseUrl}/api/dir-contents/durations?path=${encodeURIComponent(path)}`).then(res => res.json());
        return <GetDurationsResponse>json
    }

    async getMediaInfo(path: string): Promise<MediaInfo> {
        var json = await fetch(`${baseUrl}/api/media-info?path=${encodeURIComponent(path)}`).then(res => res.json());
        return <MediaInfo>json
    }

    async updateProgress(progress: number, path: string): Promise<any> {
        await fetch(`${baseUrl}/api/update-progress?progress=${progress}&path=${encodeURIComponent(path)}`, {method: "POST"});
        return;
    }

    async getSettings(): Promise<Settings> {
        return await fetch(`${baseUrl}/api/settings`).then(res => res.json());
    }

    async updateSettings(settings: Settings): Promise<any> {
        await fetch(`${baseUrl}/api/update-settings`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(settings)
        });
    }

    async uploadFiles(file: File, path: string): Promise<any> {
        const formData = new FormData();
        formData.append('file', file);
        await fetch(`${baseUrl}/api/upload-files?path=${encodeURIComponent(path)}`, {
            method: "POST", 
            body: formData
        });
    }

    async createDir(path: string): Promise<any> {
        return await fetch(`${baseUrl}/api/create-dir?path=${encodeURIComponent(path)}`, {method: "POST"});
    }

    async copyItems(request: CopyItemsRequest): Promise<any> {
        await fetch(`${baseUrl}/api/copy-items`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(request)
        });
    }

    async deleteItems(request: DeleteItemsRequest): Promise<any> {
        await fetch(`${baseUrl}/api/delete-items`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(request)
        });
    }

    async rename(path: string, newName: string): Promise<any> {
        return await fetch(`${baseUrl}/api/rename?path=${encodeURIComponent(path)}&newName=${encodeURIComponent(newName)}`, {method: "POST"});
    }

    async getUrlInfo(url: string): Promise<UrlInfo> {
        return await fetch(`${baseUrl}/api/get-url-info?url=${encodeURIComponent(url)}`).then(res => res.json());
    }

    async uploadFromUrl(request: UploadFromUrlRequest): Promise<any> {
        return await fetch(`${baseUrl}/api/upload-from-url`, {
            method: "POST", 
            headers: {'Accept': 'application/json','Content-Type': 'application/json'},
            body: JSON.stringify(request)});
    }
}

export var apiAccess = new ApiAccess()