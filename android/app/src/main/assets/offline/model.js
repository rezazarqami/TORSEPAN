/* Pure projection of a server snapshot plus the durable local outbox. No networking or credentials. */
(function(root) {
  'use strict';
  const labels={2:'در انتظار دیمپل',4:'در انتظار شیپ',6:'در انتظار پخت',8:'در انتظار تیون',10:'در انتظار چسب',11:'اتاق چسب',12:'در انتظار فاین‌تیون',14:'در انتظار QC',16:'در انتظار بسته‌بندی',18:'انبار',20:'فروخته‌شده',21:'در انتظار بسته‌بندی صادرات',22:'انبار صادرات'};
  const actions={2:'dimple',4:'shape',6:'bake',8:'tune',10:'glue',11:'release',12:'finalTune',14:'qc',16:'packaging',21:'exportPackaging',18:'sell',22:'ship'};
  const names={dimple:'ثبت دیمپل',shape:'ثبت شیپ',bake:'ثبت پخت',tune:'ثبت تیون',glue:'ثبت چسب و مونتاژ',release:'خروج از اتاق چسب',finalTune:'ثبت فاین‌تیون',qc:'ثبت کنترل کیفیت',create:'ساخت کاسه',stock:'تغییر موجودی',packaging:'بسته‌بندی',sell:'فروش ساز',ship:'ارسال کاسه صادراتی',note:'یادداشت',photo:'عکس ساز',design:'دیزاین',edit:'اصلاح شناسنامه',tuneExport:'تیون صادراتی',exportRoute:'ارسال به بسته‌بندی صادرات',returnGlue:'بازگشت به چسب',exportPackaging:'بسته‌بندی صادرات',warehouseTransfer:'انتقال انبار'};
  const roles={dimple:['Dimpler','Shaper'],shape:['Shaper'],bake:['Workshop'],tune:['Tuner'],glue:['Workshop'],release:['Workshop'],finalTune:['FineTuner'],qc:['QualityControl'],packaging:['Workshop'],exportPackaging:['Workshop'],sell:['SalesAdmin'],ship:['SalesAdmin'],create:[],stock:[],edit:[],warehouseTransfer:[],note:['*'],photo:['*'],design:['*'],tuneExport:['Tuner'],exportRoute:['Workshop'],returnGlue:['Workshop']};
  function project(snapshot, rows) {
    if(!snapshot) return [];
    const bowls=JSON.parse(JSON.stringify(snapshot.bowls));
    bowls.forEach(b=>b.localHistory=[]);
    for(const row of rows) {
      const op=row.body;
      if(op.action==='create')bowls.push({code:op.code,id:row.id,type:op.bowlType,hasNotes:op.bowlType===1||op.hasNotes,materialId:op.materialId,isCustom:false,scaleId:null,
        stage:op.bowlType===2&&!op.hasNotes?6:2,stageName:labels[op.bowlType===2&&!op.hasNotes?6:2],version:'new',pairCode:null,instrumentCode:null,localHistory:[]});
      const b=bowls.find(x=>x.code===op.code); if(!b)continue;
      const action=op.action;
      if(action==='qc'&&op.approved||action==='warehouseTransfer')for(const target of bowls.filter(x=>x.code===b.code||x.code===b.pairCode))target.qualityWarehouseId=op.qualityWarehouseId||1;
      if(action==='qc'&&op.approved)for(const target of bowls.filter(x=>x.code===b.code||x.code===b.pairCode))target.packagingExportLocation=op.exportLocation||null;
      let stage={dimple:4,shape:6,bake:b.type===2&&!b.hasNotes?10:8,tune:10,glue:11,release:12,finalTune:14,qc:op.approved?16:12,packaging:(b.packagingExportLocation||op.exportLocation)?22:18,sell:20,ship:20,tuneExport:21,exportRoute:21,returnGlue:10,exportPackaging:22}[action];
      if(action==='design'){const name=(snapshot.designs||[]).find(x=>x.id===op.designTypeId)?.name||'دیزاین';b.design=b.design?b.design+'، '+name:name;}
      if(action==='edit'&&op.edit){
        for(const [code,scaleId] of Object.entries(op.editScales||{})){const target=bowls.find(x=>x.code===code);if(target)target.scaleId=scaleId;}
        for(const update of op.edit.bowlScales||[]){const target=bowls.find(x=>x.id===update.bowlId);if(target)target.scaleId=update.scaleId;}
        if(b.instrumentCode)bowls.filter(x=>x.code===b.code||x.code===b.pairCode).forEach(x=>x.instrumentScaleId=op.edit.handpanScaleId);
      }
      if(!stage){b.localHistory.push(row);continue;}

      if(action==='shape'){b.scaleId=op.scaleId||null;b.noteCount=op.noteCount;b.isCustom=false;}
      if(action==='glue') {
        const pair=bowls.find(x=>x.code===op.pairCode); if(!pair)continue;
        b.pairCode=pair.code;pair.pairCode=b.code;
        b.instrumentCode=pair.instrumentCode=b.type===1?b.code:pair.code;
        b.instrumentScaleId=pair.instrumentScaleId=op.scaleId;
      }
      const targets=['glue','release','finalTune','qc','packaging','sell'].includes(action)?bowls.filter(x=>x.code===b.code||x.code===b.pairCode):[b];
      for(const target of targets) { target.stage=stage;target.stageName=labels[stage];target.localHistory.push(row); }
    }
    return bowls;
  }
  function performers(snapshot,action){ return snapshot.users.filter(u=>u.roles.includes('Administrator')||(roles[action]||[]).includes('*')||(roles[action]||[]).some(r=>u.roles.includes(r))); }
  function scales(snapshot,bowl,action,isCustom) {
    return action==='dimple'?[]:snapshot.scales;
  }
  function normalize(code){return String(code).trim().toUpperCase().replace(/[۰-۹]/g,c=>String(c.charCodeAt(0)-1776)).replace(/[٠-٩]/g,c=>String(c.charCodeAt(0)-1632));}
  function materials(snapshot,rows){
    const items=JSON.parse(JSON.stringify(snapshot?.materials||[]));
    for(const row of rows){const op=row.body,m=items.find(x=>x.id===op.materialId);
      if(op.action==='create'&&m){const key=op.bowlType===1?'topQuantity':'bottomQuantity';m[key]--;}
      if(op.action==='stock'&&m){for(const key of m.category===4?['topQuantity','bottomQuantity']:['quantity'])m[key]=op.setAbsolute?op[key]:(m[key]+op[key]);}
      if(op.action==='packaging')for(const id of op.materialIds||[]){const item=items.find(x=>x.id===id);if(item)item.quantity-=requiredQuantity(item.name);}
    }
    return items;
  }
  function requiredQuantity(name){return name.trim().replace(/ي/g,'ی')==='مثلثی'?2:1;}
  const api={project,materials,requiredQuantity,performers,scales,normalize,actions,names,labels};
  if(typeof module!=='undefined')module.exports=api; else root.OfflineModel=api;
})(typeof window==='undefined'?globalThis:window);
