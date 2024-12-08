export type DirContentsResponse = {
    folderInfos: FolderInfo[];
    mediaInfos: MediaInfo[];
}

export type MediaInfo = {
    fileName: string;
    fileSize: number;
    duration: number | null;
    modifyDate: string;
    progress: number;
    progressDate: string | null;
}

export type FolderInfo = {
    folderName: string;
    mediaDiskSize: number;
    mediaFileCount: number;
    modifyDate: string;
}

export type GetDurationsResponse = {
    path: string;
    mediaDurations: MediaDuration[];
}

export type MediaDuration = {
    fileName: string;
    duration: number;
}

export type Settings = {
    playbackSpeed: number;
    sortBy: string;
    sortDesc: boolean;
}