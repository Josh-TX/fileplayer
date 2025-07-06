<script setup lang="ts">
import { historyService } from '@/services/HistoryService';

var emits = defineEmits(['closed'])

function close() {
    emits("closed");
}
function toDateTime(ms: number): string {
    const date = new Date(ms);

    const months = [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    const month = months[date.getMonth()];
    const day = date.getDate();
    const hours24 = date.getHours();
    const minutes = date.getMinutes();

    // Determine suffix
    const suffix = (day >= 11 && day <= 13) ? 'th' :
        (day % 10 === 1) ? 'st' :
            (day % 10 === 2) ? 'nd' :
                (day % 10 === 3) ? 'rd' : 'th';

    const hours12 = hours24 % 12 || 12;
    const ampm = hours24 >= 12 ? 'pm' : 'am';
    const paddedMinutes = minutes.toString().padStart(2, '0');

    return `${month} ${day}${suffix}, ${hours12}:${paddedMinutes}${ampm}`;
}

function toPlaybackPos(s: number | undefined): string {
    if (!s) {
        return "";
    }
    const hours = Math.floor(s / 3600);
    const minutes = Math.floor((s % 3600) / 60);
    const seconds = Math.round(s % 60);
    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${pad(hours)}:${pad(minutes)}:${pad(seconds)}`;
}

</script>

<template>
    <div class="modal-overlay">
        <div class="modal-container">
            <div class="modal">
                <div style="max-height: 50vh; overflow-y: scroll">
                    <table>
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>From</th>
                                <th>To</th>
                                <th>Type</th>
                                <th>Path</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="e of historyService.GetEvents()" style="margin-top: 6px;">
                                <td>{{ toDateTime(e.updateMs) }}</td>
                                <td>{{ toPlaybackPos(e.initialTime) }}</td>
                                <td>{{ toPlaybackPos(e.currentTime) }}</td>
                                <td>{{ e.type }}</td>
                                <td>{{ e.path }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div style="display: flex; justify-content: end; margin-top: 6px;">
                    <button @click="close">close</button>
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
td{
    white-space: nowrap;
    padding: 2px 8px;
}
</style>