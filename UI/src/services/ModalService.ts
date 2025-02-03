import { ref } from "vue";
import { apiAccess } from "./ApiAccess";

class ModalService {
    isModalOpen = ref<boolean>(false);
    isNewFolder = ref<boolean>(false);
    currentPath = ref<string[]>([]);
    initialPath = ref<string[]>([]);
    files = ref<string[]>([]);
    isMove = ref(false);
    refreshFunc: Function | undefined;

    startMove(currentPath: string[], files: string[], isMove: boolean){
        this.currentPath.value = currentPath;
        this.initialPath.value = [...currentPath];
        this.files.value = files;
        this.isMove.value = isMove;
        this.isModalOpen.value = true;
    }
    async save(){
        await apiAccess.copyItems({
            filePaths: this.files.value,
            destinationDir: this.currentPath.value.join("/"),
            isMove: this.isMove.value
        });
        if (this.refreshFunc){
            this.refreshFunc();
        }
        this.close();
    }
    close(){
        this.isModalOpen.value = false;
    }
    registerOnRefresh(func: Function){
        this.refreshFunc = func;
    }
}


export var modalService = new ModalService();