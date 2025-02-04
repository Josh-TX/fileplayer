import { ref } from "vue";
import { apiAccess } from "./ApiAccess";

class ModalService {
    isModalOpen = ref<boolean>(false);
    isNewFolder = ref<boolean>(false);
    isSaving = ref<boolean>(false);
    currentPath = ref<string[]>([]);
    initialPath = ref<string[]>([]);
    files = ref<string[]>([]);
    isMove = ref(false);
    refreshFunc: Function | undefined;
    folderCount = ref<number>(0);

    startMove(currentPath: string[], files: string[], folderCount: number, isMove: boolean){
        this.currentPath.value = currentPath;
        this.initialPath.value = [...currentPath];
        this.files.value = files;
        this.isMove.value = isMove;
        this.isModalOpen.value = true;
        this.isNewFolder.value = false;
        this.folderCount.value = folderCount;
    }
    async save(){
        var currentPathStr = this.currentPath.value.join("/");
        if (currentPathStr == this.initialPath.value.join("/")){
            alert("destination path is same as the current path");
            return;
        }
        if (this.files.value.some(z => currentPathStr.startsWith(z))){
            alert("cannot " + (this.isMove.value ? "move" : "copy") + " a folder to within itself");
            return;
        }
        this.isSaving.value = true;
        await apiAccess.copyItems({
            filePaths: this.files.value,
            destinationDir: currentPathStr,
            isMove: this.isMove.value
        });
        this.isSaving.value = false;
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