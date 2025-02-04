<script setup lang="ts">
import { ref, shallowRef, watch } from 'vue';
import FileDrop from './FileDrop.vue';
import { pathService } from '@/services/PathService';
import { apiAccess } from '@/services/ApiAccess';
import type { UrlInfo } from '@/models/models';
import { settingsService } from '@/services/SettingsService';
import { formatDuration } from '@/services/MiscHelpers';

var emits = defineEmits(['uploaded'])
const open = ref(false)
const url = ref("");
const format = ref<number | null>(720);
const infoMsg = ref<string>("no info loaded");
const useMDate = ref(false);
const overrideName = ref("");
const urlInfo = shallowRef<UrlInfo | null>(null);
const displayPath = pathService.getPathString() || "root";
settingsService.getSettingsAsync().then(z => {
    format.value = z.preferredHeight
    useMDate.value = z.useMDate
});

async function upload() {
    if (!url.value.startsWith("http")){
        alert("url must start with http");
        return;
    }
    apiAccess.uploadFromUrl({
        path: pathService.getPathString(),
        preferredHeight: format.value,
        url: url.value,
        overrideName: overrideName.value,
        useMDate: useMDate.value
    }).then(() => {
        setTimeout(() => {
            emits("uploaded");
            close();
        }, 100)
    })

}

async function loadInfo(){
    if (!url.value.startsWith("http")){
        alert("url must start with http");
        return;
    }
    var originalUrl = url.value;
    infoMsg.value = "loading info";
    try {
        var loadedUrlInfo = await apiAccess.getUrlInfo(url.value)
        if (originalUrl == url.value){
            urlInfo.value = loadedUrlInfo;
        }
    } catch {
        infoMsg.value = "Error loading info. yt-dlp is likely unable to download from that link"
    }
}

function getFormats(urlInfo: UrlInfo){
    if (urlInfo.audioOnly){
        return "audio only";
    } else {
        return urlInfo.heights.map(z => z + "p").join(", ")
    }
}

function getDate(urlInfo: UrlInfo){
    const options = { month: 'short', day: 'numeric', year: 'numeric' };
    return new Date(urlInfo.publishDate).toLocaleDateString('en-US', <any>options);
}

function close(){
    url.value = "";
    overrideName.value = "";
    urlInfo.value = null;
    open.value = false;
}

watch(format, (newVal, oldVal) => {
    settingsService.updatePreferredHeight(newVal)
});
watch(useMDate, (newVal, oldVal) => {
    settingsService.updateUseMDate(newVal)
});

watch(url, (newVal, oldVal) => {
    infoMsg.value = "no info loaded";
    urlInfo.value = null;
});

</script>

<template>
    <button @click="open = true">Upload From Url</button>
    <div class="modal-overlay" v-if="open">
        <div class="modal-container">
            <div class="modal">
                <h3>Upload from URL to {{ displayPath }}</h3>
                <p>The server will use yt-dlp to download media in the background. yt-dlp is supports a variety of websites and media formats</p>
                <div style="display: flex; gap: 12px;">
                    <input placeholder="url" style="flex: 1 0 0; padding: 8px 6px;" v-model="url">
                    <button :disabled="!url" @click="loadInfo">load info</button>
                </div>
                <div class="options-container">
                    <span>Preferred Format</span>
                    <div>
                        <select v-model="format">
                            <option :value="null">audio only</option>
                            <option :value="240">240p</option>
                            <option :value="360">360p</option>
                            <option :value="480">480p</option>
                            <option :value="720">720p</option>
                            <option :value="1080">1080p</option>
                        </select>
                    </div>
                    <span>Override Filename</span>
                    <input placeholder="(don't override)" style="padding: 2px;" v-model="overrideName">
                </div>
                <div style="margin-top: 4px">
                    <input v-model="useMDate" type="checkbox">
                    Set file date to publish date (if available)
                </div>
                <div class="info">
                    <div v-if="!urlInfo" class="text-muted" style="padding-top: 30px; text-align: center;">{{ infoMsg }}</div>
                    <div v-else style="display: flex;">
                        <div v-if="urlInfo.thumbnailUrl" style="height: 100%; width: 35vh;">
                            <img :src="urlInfo.thumbnailUrl" style="height: 20vh; width: 30vh; object-fit: contain;">
                        </div>
                        <div>
                            <h3>{{ urlInfo.title }}</h3>
                            <div class="options-container">
                                <span>duration:</span>
                                <span v-if="urlInfo.duration">{{ formatDuration(urlInfo.duration) }}</span>
                                <span v-if="!urlInfo.duration" class="text-muted">(not available)</span>
                                <span>formats:</span>
                                <span> {{ getFormats(urlInfo) }}</span>
                                <span>publish date:</span>
                                <span v-if="urlInfo.publishDate">{{ getDate(urlInfo) }}</span>
                                <span v-if="!urlInfo.publishDate" class="text-muted">(not available)</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div style="display: flex; justify-content: space-between; margin-top: 6px;">
                    <button @click="close">close</button>
                    <button :disabled="!url" @click="upload">queue upload</button>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
@keyframes fadeIn {
    0% {
        opacity: 0;
    }

    100% {
        opacity: 1;
    }
}

.modal-container {
    max-width: 800px;
    margin: 15vh auto 0 auto;
    padding: 0 12px;
}

.modal {
    background: var(--bg-default);
    border: 1px solid var(--dropdown-border);
    padding: 8px 16px 16px 16px;
    box-shadow: 4px 4px 16px 2px rgba(0, 0, 0, 0.5);
}

.modal-overlay {
    position: fixed;
    z-index: 100;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    background-color: rgba(0, 0, 0, 0.4);
    animation: fadeIn 0.4s;
}
.options-container{
    margin-top: 8px;
    display: grid;
    grid-template-columns: max-content 1fr;
    gap: 8px 12px;
    align-items: center;
}
.info{
    height: 20vh;
}
</style>