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
    foldersFirst: boolean,
    useMDate: boolean;
    preferredHeight: number | null;
    compatCodec: boolean;
    useNative: boolean;
}

export type CopyItemsRequest = {
    filePaths: string[];
    destinationDir: string;
    isMove: boolean;
}

export type DeleteItemsRequest = {
    filePaths: string[];
}

export type UrlInfo = {
    title: string;
    duration: number;
    thumbnailUrl: string;
    heights: string[];
    audioOnly: boolean;
    publishDate: string;
}

export type UploadFromUrlRequest = {
    path: string;
    url: string;
    preferredHeight: number | null;
    overrideName: string;
    useMDate: boolean;
    compatCodec: boolean;
}