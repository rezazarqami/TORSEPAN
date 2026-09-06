window.handpanPhotos = (() => {
    const refs = new Map(), pending = new Map(), notifying = new Set(), busy = new Set();
    const retryCounts = new Map(), retryTimers = new Map();
    const status = (id, text) => { const el = document.getElementById(id+"-status"); if (el) el.textContent = text; };
    async function convert(file) {
        const bitmap = await createImageBitmap(file, {imageOrientation:"from-image"});
        try {
            const render = (w,h,q,crop) => {
                let sx=0,sy=0,sw=bitmap.width,sh=bitmap.height;
                if(crop){const side=Math.min(sw,sh);sx=(sw-side)/2;sy=(sh-side)/2;sw=sh=side;}
                else{const scale=Math.min(1,w/sw,h/sh);w=Math.max(1,Math.round(sw*scale));h=Math.max(1,Math.round(sh*scale));}
                const canvas=document.createElement("canvas");canvas.width=w;canvas.height=h;
                canvas.getContext("2d",{alpha:false}).drawImage(bitmap,sx,sy,sw,sh,0,0,w,h);
                return new Promise((resolve,reject)=>canvas.toBlob(blob=>blob?.type==="image/webp"?resolve(blob):reject(new Error("تبدیل عکس به WebP انجام نشد.")),"image/webp",q));
            };
            return {name:file.name||"photo",full:await render(2400,2400,.86,false),thumb:await render(320,320,.8,true)};
        } finally {bitmap.close();}
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
        optimize:async id=>{
            const input=document.getElementById(id),out=[];
            for(const file of Array.from(input.files)){
                const p=await convert(file);
                out.push({name:p.name,image:new Uint8Array(await p.full.arrayBuffer()),thumbnail:new Uint8Array(await p.thumb.arrayBuffer())});
            }
            input.value="";return out;
        },
        uploadCamera:async(input,handpanId)=>{
            const file=input.files?.[0],id=input.id;if(!file||busy.has(id))return;
            busy.add(id);input.disabled=true;
            const label=input.closest("label"),caption=label?.querySelector("[data-camera-label]");
            label?.classList.add("disabled");if(caption)caption.textContent="ثبت…";
            status(id,"در حال آماده‌سازی عکس…");let timeout;
            try{
                const p=await convert(file),form=new FormData();
                form.append("file",p.full,"camera-photo.webp");form.append("thumbnail",p.thumb,"camera-photo-thumb.webp");
                status(id,"در حال ارسال عکس…");
                const controller=new AbortController();timeout=setTimeout(()=>controller.abort(),120000);
                const response=await fetch("/api/internal/handpans/"+encodeURIComponent(handpanId)+"/photos",{
                    method:"POST",headers:{Authorization:"Bearer "+(localStorage.getItem("access_token")||"")},body:form,signal:controller.signal
                });
                if(!response.ok)throw new Error(response.status===401?"نشست ورود پایان یافته؛ دوباره وارد حساب شوید.":"ثبت عکس انجام نشد؛ وضعیت گالری را بررسی کنید.");
                const result=await response.json();pending.set(id,result.id);
                status(id,"عکس ثبت شد؛ در حال تازه‌کردن گالری…");await notify(id);
            }catch(error){status(id,error.name==="AbortError"?"پاسخ ارسال دریافت نشد؛ قبل از ارسال دوباره گالری را بررسی کنید.":error.message||"ثبت عکس انجام نشد.");}
            finally{clearTimeout(timeout);busy.delete(id);input.disabled=false;input.value="";label?.classList.remove("disabled");if(caption)caption.textContent="دوربین";}
        }
    };
})();
