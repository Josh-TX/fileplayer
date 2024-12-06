import type { DirContentsResponse } from "@/models/models";
const baseUrl = import.meta.env.VITE_BASE_URL;

class ApiAccess {

    async getDirContents(path: string): Promise<DirContentsResponse> {
        var json = await fetch(`${baseUrl}/api/dir-contents?path=${encodeURIComponent(path)}`).then(res => res.json());
        return <DirContentsResponse>json
    }
}

export var apiAccess = new ApiAccess()