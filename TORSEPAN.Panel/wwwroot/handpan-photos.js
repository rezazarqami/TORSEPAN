window.handpanPhotos = (() => {
    const refs = new Map(), pending = new Map(), notifying = new Set(), busy = new Set();
    const retryCounts = new Map(), retryTimers = new Map();
    const status = (id, text) => { const el = document.getElementById(id+"-status"); if (el) el.textContent = text; };
    async function decode(file) {
        if (typeof createImageBitmap === "function") {
            try { return await createImageBitmap(file, {imageOrientation:"from-image"}); } catch { }
        }
        const url=URL.createObjectURL(file), image=new Image();
        try {
            await new Promise((resolve,reject)=>{image.onload=resolve;image.onerror=()=>reject(new Error("این عکس در مرورگر قابل بازشدن نیست؛ نسخه JPEG یا PNG آن را انتخاب کنید."));image.src=url;});
            return image;
        } finally { URL.revokeObjectURL(url); }
    }
    async function convert(file) {
        const bitmap = await decode(file);
        try {
            const render = (w,h,q,crop,type) => {
                let sx=0,sy=0,sw=bitmap.width,sh=bitmap.height;
                if(crop){const side=Math.min(sw,sh);sx=(sw-side)/2;sy=(sh-side)/2;sw=sh=side;}
                else{const scale=Math.min(1,w/sw,h/sh);w=Math.max(1,Math.round(sw*scale));h=Math.max(1,Math.round(sh*scale));}
                const canvas=document.createElement("canvas");canvas.width=w;canvas.height=h;
                const context=canvas.getContext("2d",{alpha:false});
                if(!context)throw new Error("آماده‌سازی عکس در مرورگر انجام نشد.");
                context.fillStyle="#fff";context.fillRect(0,0,w,h);
                context.drawImage(bitmap,sx,sy,sw,sh,0,0,w,h);
                return new Promise((resolve,reject)=>canvas.toBlob(blob=>blob?resolve(blob):reject(new Error("تبدیل عکس انجام نشد.")),type,q));
            };
            let full=await render(2400,2400,.86,false,"image/webp");
            // Some mobile browsers decode images but cannot encode WebP.
            if(full.type!=="image/webp")full=await render(2400,2400,.86,false,"image/jpeg");
            const thumb=await render(320,320,.8,true,full.type);
            if(!["image/webp","image/jpeg"].includes(full.type)||thumb.type!==full.type)
                throw new Error("فرمت خروجی عکس در این مرورگر پشتیبانی نمی‌شود.");
            if(full.size>5000000||thumb.size>500000)throw new Error("حجم عکس زیاد است؛ عکس کوچک‌تری انتخاب کنید.");
            return {full,thumb};
        } finally {bitmap.close?.();}
    }
    async function notify(id) {
        if(!pending.has(id)||!refs.has(id)||notifying.has(id)||window.torsepanConnection?.connected===false)return;
        notifying.add(id);const photoId=pending.get(id);
        try{
            await refs.get(id).invokeMethodAsync("CameraUploaded",photoId);
            if(pending.get(id)===photoId)pending.delete(id);
            retryCounts.delete(id);clearTimeout(retryTimers.get(id));retryTimers.delete(id);
            status(id,"عکس با موفقیت ثبت شد.");
        }catch{
            status(id,"عکس ثبت شد؛ پس از اتصال، گالری همین‌جا تازه می‌شود.");
            const count=retryCounts.get(id)||0;
            if(count<3 && refs.has(id)){
                retryCounts.set(id,count+1);
                clearTimeout(retryTimers.get(id));
                retryTimers.set(id,setTimeout(()=>{retryTimers.delete(id);void notify(id);},3000));
            }
        }
        finally{notifying.delete(id);}
    }
    window.addEventListener("torsepan-connected",()=>{for(const id of pending.keys())void notify(id);});
    document.addEventListener("visibilitychange",()=>{if(!document.hidden)for(const id of pending.keys())void notify(id);});
    return {
        get hasPending(){return busy.size>0||pending.size>0;},
        register(id,ref){refs.set(id,ref);void notify(id);},
        unregister(id){refs.delete(id);pending.delete(id);retryCounts.delete(id);clearTimeout(retryTimers.get(id));retryTimers.delete(id);},
        uploadCamera:(input,handpanId)=>upload(input,handpanId,1),
        uploadGallery:(input,handpanId)=>upload(input,handpanId,8)
    };
    async function upload(input,handpanId,limit) {
        const files=Array.from(input.files||[]),id=input.id;
        if(!files.length||busy.has(id))return;
        if(files.length>limit){status(id,"حداکثر "+limit+" عکس را در هر بار انتخاب کنید.");input.value="";return;}
        const label=input.closest("label"),caption=label?.querySelector("[data-camera-label],[data-gallery-label]"),original=caption?.textContent;
        busy.add(id);input.disabled=true;label?.classList.add("disabled");if(caption)caption.textContent="ثبت…";
        let completed=0;
        try {
            for(const file of files) {
                status(id,"در حال آماده‌سازی عکس "+(completed+1)+" از "+files.length+"…");
                const p=await convert(file),form=new FormData(),extension=p.full.type==="image/webp"?"webp":"jpg";
                form.append("file",p.full,"photo."+extension);form.append("thumbnail",p.thumb,"photo-thumb."+extension);
                const controller=new AbortController(),timeout=setTimeout(()=>controller.abort(),120000);
                try {
                    status(id,"در حال ارسال عکس "+(completed+1)+" از "+files.length+"…");
                    const response=await fetch("/api/internal/handpans/"+encodeURIComponent(handpanId)+"/photos",{
                        method:"POST",headers:{Authorization:"Bearer "+(localStorage.getItem("access_token")||"")},body:form,signal:controller.signal
                    });
                    if(!response.ok){
                        const text=await response.text();let message;
                        try{const data=JSON.parse(text);if(typeof data==="string")message=data;}catch{if(text.length<250&&!text.includes("<"))message=text;}
                        throw new Error(response.status===401?"نشست ورود پایان یافته؛ دوباره وارد حساب شوید.":message||"ثبت عکس انجام نشد ("+response.status+").");
                    }
                    const result=await response.json();completed++;pending.set(id,result.id);
                    status(id,"عکس ثبت شد؛ در حال تازه‌کردن گالری…");await notify(id);
                } finally {clearTimeout(timeout);}
            }
            if(!pending.has(id))status(id,completed+" عکس با موفقیت ثبت شد.");
        } catch(error) {
            const prefix=completed?completed+" عکس ثبت شد؛ ":"";
            status(id,prefix+(error.name==="AbortError"?"پاسخ ارسال دریافت نشد؛ قبل از ارسال دوباره گالری را بررسی کنید.":error.message||"ثبت عکس انجام نشد."));
        } finally {busy.delete(id);input.disabled=false;input.value="";label?.classList.remove("disabled");if(caption)caption.textContent=original;}
    }
})();
