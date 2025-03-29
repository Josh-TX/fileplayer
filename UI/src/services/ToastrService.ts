import { ref } from "vue";
import { apiAccess } from "./ApiAccess";

class ToastrService {
    showError(message: string){
        var div = document.createElement("div");
        var div2 = document.createElement("div");
        div.classList.add("toast-container");
        div2.classList.add("toast", "toast-error");
        div2.innerText = message;
        div.appendChild(div2);
        document.body.appendChild(div);
        setTimeout(() => {
            div.classList.add("fadeout");
            setTimeout(() => {
                document.body.removeChild(div);
            }, 700)
        }, 5000)
    }
}


export var toastrService = new ToastrService();