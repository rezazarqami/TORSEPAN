window.torsepanWarehouse={focusTab:function(id){document.getElementById('warehouse-tab-'+id)?.focus();}};

window.warehousePreviewBack=(()=>{
 let active=false,callback;
 function dismiss(){document.querySelectorAll('.gallery-preview-overlay').forEach(x=>x.style.display='none');active=false;if(callback)callback.invokeMethodAsync("PreviewWentBack").catch(()=>{});}
 window.addEventListener('popstate',()=>{if(active)dismiss();});
 return {open(reference){callback=reference;document.querySelectorAll('.gallery-preview-overlay').forEach(x=>x.style.display='');if(!active){history.pushState({...history.state,torsepanPhotoPreview:true},'',location.href);active=true;}},close(){if(active){active=false;history.back();}},release(){active=false;callback=null;}};
})();
