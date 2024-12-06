export type DirContentsResponse = {
    directories: string[];
    mediaInfos: MediaInfo[];
}

export type MediaInfo = {
    fileName: string;
    fileSize: number;
    duration: number | null;
    progress: number;
    lastDate: string | null;
}