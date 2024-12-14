
export function formatDuration(secondsCount: number | null){
    if (secondsCount == null){
        return "\xa0\xa0"
    }
    const hours = Math.floor(secondsCount / 3600);
    const minutes = Math.floor((secondsCount % 3600) / 60);
    const seconds = secondsCount % 60;

    return [
        hours > 0 ? String(hours) : null, // Include hours only if greater than 0
        String(minutes).padStart(hours > 0 ? 2 : 1, '0'), // Ensure minutes are padded if hours are included
        String(seconds).padStart(2, '0') // Always pad seconds
    ].filter(Boolean).join(':'); // Filter out `null` and join with colons
}