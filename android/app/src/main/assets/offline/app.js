'use strict';
const model=OfflineModel,$=id=>document.getElementById(id);
const escape=value=>String(value??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const date=value=>{try{return new Intl.DateTimeFormat('fa-IR',{dateStyle:'short',timeStyle:'short'}).format(new Date(value));}catch(_){return value;}};
let state={},bowls=[],selected='',formStage=0,extraAction='';
const option=(value,name,current)=>`<option value="${escape(value)}" ${value===current?'selected':''}>${escape(name)}</option>`;
const select=(id,label,items,current,required=true)=>`<label for="${id}">${label}</label><select id="${id}" ${required?'required':''}>${option('','انتخاب کنید',current)}${items.map(x=>option(x.id,x.name,current)).join('')}</select>`;
function contributors(text){return text.replace(/\|CONTRIB:(.*)/,(_,values)=>' | '+values.split(';').map(v=>{const [kind,id]=v.split('=');return ({Stretch:'کشش',NoteArea:'دورنوت',Edit:'اصلاح'}[kind]||kind)+' توسط '+(state.snapshot.users.find(u=>u.id===id)?.name||id);}).join('، '));}
function pendingHistory(row){const op=row.body;const source=bowls.find(x=>x.code===op.code);const bowlLabel=['dimple','shape','bake','tune'].includes(op.action)?(source?.type===1?'کاسه رو ':'کاسه زیر ')+op.code:'ساز / مونتاژ';let details='';for(const [key,name] of [['stretchUserId','کشش'],['noteAreaUserId','دورنوت'],['editUserId','اصلاح']])if(op[key])details+=' | '+name+' توسط '+(state.snapshot.users.find(u=>u.id===op[key])?.name||'—');return `<div class="history">${escape(model.names[op.action])} · ${escape(bowlLabel)} · ${escape(state.snapshot.users.find(u=>u.id===op.performerId)?.name||'—')}${escape(details)}<small>${escape(date(op.occurredUtc))} · ${row.state==='applied'?'در سرور ثبت شد؛ دریافت اطلاعات تازه در انتظار':'ثبت محلی؛ هنوز تأیید نشده'}</small></div>`;}
function renderPassport(saved={}){
 const b=bowls.find(x=>x.code===selected);if(!b){$('passport').innerHTML='';return;}
 const snapshot=state.snapshot,record=snapshot.passports.records.find(x=>x.code===(b.instrumentCode||b.code));
 const baseRecord=record||snapshot.passports.records.find(x=>x.code===b.code);
 const scale=snapshot.scales.find(x=>x.id===b.instrumentScaleId)?.name||(b.instrumentCode?baseRecord?.scale:snapshot.scales.find(x=>x.id===b.scaleId)?.name)||baseRecord?.scale||'ثبت نشده';
 const pair=bowls.find(x=>x.code===b.pairCode); const kind=b.instrumentCode?'ساز':b.type===1?'کاسه رو':'کاسه زیر';
 let html=`<section class="card"><div class="identity"><small>TORSEPAN · ${b.instrumentCode?'INSTRUMENT':'BOWL'} ID</small><p>${b.instrumentCode?'کد ساز':'کد کاسه'}: <b dir="ltr">${escape(b.instrumentCode||b.code)}</b>${pair?' · '+escape(b.type===1?'کاسه زیر':'کاسه رو')+': '+escape(pair.code):''}</p>${!b.instrumentCode?`<p>نوع کاسه: ${kind}</p>`:''}<p><span dir="ltr">Scale: ${escape(scale)}</span></p><p><span dir="ltr">Design:</span> ${escape(b.design||baseRecord?.design||'ساده')}</p><p><span dir="ltr">Status:</span> ${escape(b.stageName)}</p></div><h2>مراحل ثبت‌شده</h2>`;
 let history=baseRecord?.operations||[];
 if(!record&&b.instrumentCode&&pair){const other=snapshot.passports.records.find(x=>x.code===pair.code);history=[...history,...(other?.operations||[])].sort((x,y)=>new Date(x.date)-new Date(y.date));}
 for(const row of state.operations||[])if(row.body.action==='edit')for(const update of row.body.edit?.eventUsers||[])history=history.map(h=>h.id===update.eventId?{...h,actor:snapshot.users.find(u=>u.id===update.userId)?.name||h.actor}:h);
 html+=history.map(h=>`<div class="history">${escape(h.action)} · ${escape(h.bowl)} · ${escape(h.actor)}<small>${escape(date(h.date))} · ${escape(h.duration)} · ${escape(contributors(h.description))}</small></div>`).join('');
 const related=new Set([...b.localHistory,...(b.instrumentCode?pair?.localHistory||[]:[])].map(x=>x.id));
 html+=(state.operations||[]).filter(x=>related.has(x.id)).map(pendingHistory).join('');
 const action=b.isRejected?undefined:({2:'dimple',4:'shape',6:'bake',8:'tune',10:'glue',11:'release',12:'finalTune',14:'qc'})[b.stage];
 if(action&&!state.locked){html+=`<h2>${escape(model.names[action])}</h2><form id="operation">${select('performerId','انجام‌دهنده',model.performers(snapshot,action).map(u=>({id:u.id,name:u.name})),saved.performerId)}<div id="fields">`;
 if(['dimple','shape','tune','finalTune'].includes(action))html+=select('duration','مدت انجام مرحله',Array.from({length:13},(_,i)=>({id:String(i+1),name:i===12?'بیش از ۶۰ دقیقه':`${(i+1)*5} دقیقه`})),saved.duration);
 if(action==='dimple')html+=`<label><input id="isCustom" type="checkbox" ${saved.isCustom?'checked':''}>اسکیل سفارشی (انتخاب در مرحله شیپ)</label>`;
 if(action==='glue') html+=select('pairCode','کاسه مقابل',bowls.filter(x=>x.stage===10&&x.type!==b.type&&!x.pairCode).map(x=>({id:x.code,name:x.code})),saved.pairCode);
 if(action==='dimple'||action==='shape'&&b.isCustom||action==='glue')html+=select('scaleId','Scale',model.scales(snapshot,action==='glue'&&b.type===2?bowls.find(x=>x.code===saved.pairCode)||b:b,action,!!saved.isCustom),saved.scaleId);
 if(action==='shape'){const shapers=snapshot.users.filter(u=>u.roles.includes('Shaper')).map(u=>({id:u.id,name:u.name}));for(const [key,name] of [['stretchUserId','کشش توسط'],['noteAreaUserId','دورنوت توسط'],['editUserId','اصلاح توسط']])html+=select(key,name+' (اختیاری)',shapers,saved[key],false);}
 if(action==='qc')html+=`<label><input id="approved" type="checkbox" ${saved.approved===false?'':'checked'}>تأیید کنترل کیفیت</label>${select('rejectionReason','علت رد (در صورت عدم تأیید)',[{id:'OutOfTune',name:'اشکال تیون'},{id:'Appearance',name:'اشکال ظاهری'},{id:'Other',name:'سایر'}],saved.rejectionReason,false)}<label for="details">توضیح</label><textarea id="details" maxlength="2000">${escape(saved.details||'')}</textarea>`;
 html+=`</div><button id="save" type="submit">ذخیره قطعی روی گوشی</button><p class="muted">پس از ثبت، این عملیات تا تأیید سرور در صف باقی می‌ماند.</p></form>`;
 }else html+=`<p class="muted">${state.locked?'برای ثبت جدید، وارد حساب مدیر همین گوشی شوید.':'گزینه‌های ثبت و اصلاحات در پایین نمایش داده می‌شوند.'}</p>`;
 html+=(state.operations||[]).filter(r=>r.body.action==='photo'&&[b.code,b.pairCode].includes(r.body.code)&&r.body.thumbnail).map(r=>`<img class="photo-preview" alt="عکس ذخیره‌شده روی گوشی" src="data:image/jpeg;base64,${escape(r.body.thumbnail)}">`).join('');
 $('passport').innerHTML=html+'<div id="extra-buttons" class="buttons"></div><div id="extra-form"></div></section>';formStage=b.stage;renderExtraButtons(b);
 if(action==='dimple') $('isCustom')?.addEventListener('change',()=>{const scale=$('scaleId');scale.disabled=$('isCustom').checked;scale.required=!scale.disabled;});
 if(action==='dimple'&&$('isCustom')?.checked){$('scaleId').disabled=true;$('scaleId').required=false;}
 if(action==='glue')$('pairCode').addEventListener('change',()=>{const other=bowls.find(x=>x.code===$('pairCode').value),top=b.type===1?b:other;const scales=top?model.scales(snapshot,top,action,false):[];$('scaleId').innerHTML=option('','انتخاب کنید','')+scales.map(x=>option(x.id,x.name,'')).join('');});
 $('operation')?.addEventListener('submit',event=>{
  event.preventDefault();const button=$('save');button.disabled=true;
  try{
   const op={code:b.code,action,expectedVersion:b.version,performerId:$('performerId').value};
   for(const key of ['duration','scaleId','pairCode','stretchUserId','noteAreaUserId','editUserId','rejectionReason','details'])if($(key)&&$(key).value)op[key]=key==='duration'?Number($(key).value):$(key).value;
   if($('isCustom'))op.isCustom=$('isCustom').checked;
   if($('approved'))op.approved=$('approved').checked;
   if(action==='glue'){const other=bowls.find(x=>x.code===op.pairCode);if(!other)throw Error('کاسه مقابل لازم است.');op.pairVersion=other.version;}
   if(action==='qc'&&!op.approved&&(!op.rejectionReason||op.rejectionReason==='Other'&&!op.details?.trim()))throw Error('علت رد و توضیح لازم را وارد کنید.');
   const reply=JSON.parse(OfflineNative.save(JSON.stringify(op)));if(!reply.id)throw Error(reply.error||'ذخیره انجام نشد.');window.refreshOffline();
  }catch(error){$('message').textContent=error.message;button.disabled=false;}
 });
}
function renderResults(auto=true){const query=model.normalize($('search').value),matches=query?bowls.filter(b=>model.normalize(b.code).includes(query)||b.type===1&&model.normalize(b.instrumentCode||'').includes(query)):[];
 $('results').innerHTML=matches.slice(0,40).map(b=>`<button class="result" data-code="${escape(b.code)}">${escape(b.code)} · ${escape(b.instrumentCode?'ساز':b.type===1?'کاسه رو':'کاسه زیر')} · ${escape(b.stageName)}</button>`).join('');
 if(matches.length===1){selected=matches[0].code;if(auto)renderPassport();}
}
window.refreshOffline=function(){
 const extraSaved={};document.querySelectorAll('#extra-operation input,#extra-operation select,#extra-operation textarea').forEach(e=>extraSaved[e.id]=e.type==='checkbox'?e.checked:e.value);
 const saved={};document.querySelectorAll('#operation input,#operation select,#operation textarea').forEach(e=>saved[e.id]=e.type==='checkbox'?e.checked:e.value);
 try{state=JSON.parse(OfflineNative.read());}catch(_){state={message:'خواندن اطلاعات محلی انجام نشد.'};}
 bowls=model.project(state.snapshot,state.operations||[]);$('message').textContent=state.message||'ابتدا این گوشی را هنگام اتصال به اینترنت فعال کنید.';
 const pending=(state.operations||[]).filter(x=>x.state!=='applied');$('summary').textContent=state.snapshot?`آخرین دریافت: ${date(state.snapshot.capturedUtc)} · ${bowls.length} کاسه · ${pending.length} عملیات در انتظار`:'هنوز اطلاعاتی روی گوشی دریافت نشده است.';
 $('activate').hidden=!!state.snapshot;
 $('queue').innerHTML=(state.operations||[]).map(row=>`<div class="operation">${escape(row.body.code)} · ${escape(model.names[row.body.action])}<span class="pill ${escape(row.state)}">${row.state==='applied'?'تأیید شد':row.state==='blocked'?'نیاز به بررسی':'در انتظار'}</span><small>${escape(date(row.body.occurredUtc))} · ${escape(row.message)}</small></div>`).join('');
 $('retry').hidden=!(state.operations||[]).some(x=>x.state==='blocked');
 renderResults(false);const b=bowls.find(x=>x.code===selected);const sameStage=b?.stage===formStage;renderPassport(sameStage?saved:{});if(extraAction&&sameStage)renderExtended(extraAction,b,extraSaved);else extraAction='';
};
$('search').addEventListener('input',renderResults);$('results').addEventListener('click',event=>{const item=event.target.closest('[data-code]');if(item){selected=item.dataset.code;renderPassport();}});
$('activate').addEventListener('click',()=>{$('message').textContent='در حال فعال‌سازی و دریافت اطلاعات…';OfflineNative.activate();});
$('sync').addEventListener('click',()=>{$('message').textContent='در حال ارسال صف…';OfflineNative.sync();});
$('retry').addEventListener('click',()=>{OfflineNative.retry();});

const input=(id,label,type='text',value='',required=false)=>`<label for="${id}">${label}</label><input id="${id}" type="${type}" value="${escape(value)}" ${required?'required':''} ${type==='number'?'min="0" step="any"':''} maxlength="200">`;
const locations=[{id:'1',name:'ترکیه'},{id:'2',name:'لندن'},{id:'3',name:'آلمان'},{id:'4',name:'ایران'}];
const inventory=()=>model.materials(state.snapshot,state.operations||[]);
function materialVersions(ids){const result={};for(const id of ids){const m=inventory().find(x=>x.id===id);if(!m)throw Error('متریال پیدا نشد.');result[id]=m.version;}return result;}
function saveExtended(op){
 const response=JSON.parse(OfflineNative.save(JSON.stringify(op)));if(!response.id)throw Error(response.error||'ذخیره انجام نشد.');
 extraAction='';$('workspace').innerHTML='';if(op.action==='create')selected=op.code;window.refreshOffline();
}
function renderExtraButtons(b){
 if(state.locked)return;
 const actions=['note','edit'];if(!b.isRejected){
  if(b.stage===8)actions.push('tuneExport');if(b.stage===10&&!b.instrumentCode)actions.push('exportRoute');
  if(b.stage===21)actions.push('exportPackaging','returnGlue');if(b.stage===16&&b.instrumentCode)actions.push('packaging');
  if([18,22].includes(b.stage))actions.push(b.instrumentCode?'sell':'ship');
  if(b.instrumentCode&&b.stage>=12)actions.push('photo');if(![18,20,22].includes(b.stage))actions.push('design');
 }
 $('extra-buttons').innerHTML=actions.map(a=>`<button data-extra="${a}">${escape(model.names[a])}</button>`).join('');
 $('extra-buttons').addEventListener('click',e=>{const a=e.target.dataset.extra;if(a){extraAction=a;renderExtended(a,b);}});
}
function renderExtended(action,b,saved={}){
 const snap=state.snapshot,users=model.performers(snap,action).map(x=>({id:x.id,name:x.name}));
 let html=`<h2>${escape(model.names[action])}</h2><form id="extra-operation">${select('extra-performer','انجام‌دهنده',users,saved['extra-performer']||snap.ownerId)}`;
 if(action==='packaging'){
  html+='<p>اقلام مصرفی بسته‌بندی</p>';
  for(const m of inventory().filter(x=>x.category===3))html+=`<label><input id="material-${escape(m.id)}" type="checkbox" ${saved['material-'+m.id]?'checked':''} ${m.quantity<model.requiredQuantity(m.name)?'disabled':''}>${escape(m.name)} · موجودی ${m.quantity} · مصرف ${model.requiredQuantity(m.name)}</label>`;
 }
 if(['packaging','exportPackaging'].includes(action))html+=select('extra-location',action==='packaging'?'انبار صادرات (برای انبار داخلی خالی بماند)':'انبار صادرات',locations,saved['extra-location'],action==='exportPackaging');
 if(action==='tuneExport')html+=select('extra-duration','مدت تیون',Array.from({length:13},(_,i)=>({id:String(i+1),name:i===12?'بیش از ۶۰ دقیقه':`${(i+1)*5} دقیقه`})),saved['extra-duration']);
 if(['sell','ship'].includes(action)){
  html+=select('extra-party','طرف حساب (اختیاری)',snap.parties||[],saved['extra-party'],false)+input('extra-buyer','نام خریدار','text',saved['extra-buyer'])+input('extra-phone','شماره خریدار','tel',saved['extra-phone'])+input('extra-destination','مقصد','text',saved['extra-destination']);
  if(action==='sell')html+=input('extra-price','مبلغ فروش','number',saved['extra-price'])+input('extra-received','مبلغ دریافتی','number',saved['extra-received'])+`<label><input id="extra-export" type="checkbox" ${saved['extra-export']||b.stage===22?'checked':''}>فروش صادراتی</label>`+select('extra-source','روش آشنایی',snap.leadSources||[],saved['extra-source'],false)+select('extra-referrer','معرف',snap.referrers||[],saved['extra-referrer'],false);
  else html+=input('extra-shipping','روش ارسال','text',saved['extra-shipping'])+`<label><input id="extra-settled" type="checkbox" ${saved['extra-settled']?'checked':''}>تسویه‌شده</label>`;
 }
 if(action==='note')html+=`<label>متن یادداشت<textarea id="extra-details" maxlength="2000" required>${escape(saved['extra-details'])}</textarea></label>${b.instrumentCode?'<label><input id="extra-instrument" type="checkbox" checked>یادداشت شناسنامه ساز</label>':''}`;
 if(action==='design'){const used=new Set((snap.editableEvents||[]).filter(e=>e.bowlId===b.id&&e.description?.startsWith('DESIGN:')).map(e=>e.description.split(':')[1]));for(const row of state.operations||[])if(row.body.action==='design'&&row.body.code===b.code)used.add(row.body.designTypeId);html+=select('extra-design','دیزاین',(snap.designs||[]).filter(d=>!used.has(d.id)),saved['extra-design']);}
 if(action==='edit'){
  const targets=bowls.filter(x=>x.code===b.code||x.code===b.pairCode);
  for(const target of targets)html+=select('scale-'+target.code,'Scale '+target.code,snap.scales.filter(s=>(s.usage&(target.type===1?1:2))!==0),saved['scale-'+target.code]??target.scaleId,false);
  if(b.instrumentCode)html+=select('extra-scale','Scale ساز',snap.scales.filter(s=>(s.usage&4)!==0),saved['extra-scale']??b.instrumentScaleId??snap.scales.find(s=>s.name===snap.passports.records.find(x=>x.code===b.instrumentCode)?.scale)?.id,false);
  const history=snap.passports.records.filter(r=>[b.code,b.pairCode,b.instrumentCode].includes(r.code)).flatMap(r=>r.operations);
  const ids=new Set(history.map(x=>x.id));
  for(const event of snap.editableEvents||[])if(ids.has(event.id))html+=select('event-'+event.id,'انجام‌دهنده '+(history.find(x=>x.id===event.id)?.action||'مرحله')+' · '+date(event.date),snap.users,saved['event-'+event.id]??event.userId);
 }
 html+=`<button type="submit">${action==='photo'?'انتخاب عکس و ذخیره روی گوشی':'ذخیره قطعی روی گوشی'}</button></form>`;
 $('extra-form').innerHTML=html;
 $('extra-operation').addEventListener('submit',e=>{
  e.preventDefault();try{
   const op={code:b.code,action,performerId:$('extra-performer').value,expectedVersion:b.version};
   if(action==='packaging'){op.materialIds=inventory().filter(m=>$('material-'+m.id)?.checked).map(m=>m.id);op.materialVersions=materialVersions(op.materialIds);const cases=inventory().filter(m=>op.materialIds.includes(m.id)&&['سافت کیس','میدل کیس','هارد کیس','soft case','middle case','hard case'].includes(m.name.trim().toLowerCase()));if(cases.length>1)throw Error('یک نوع کیس انتخاب کنید.');}
   if($('extra-location')?.value)op.exportLocation=Number($('extra-location').value);
   if($('extra-duration'))op.duration=Number($('extra-duration').value);
   for(const [key,id] of [['buyerName','buyer'],['buyerPhoneNumber','phone'],['destination','destination'],['partyId','party'],['leadSourceId','source'],['referrerId','referrer'],['shippingMethod','shipping'],['designTypeId','design']])if($('extra-'+id)?.value)op[key]=$('extra-'+id).value;
   for(const [key,id] of [['price','price'],['receivedAmount','received']])if($('extra-'+id)?.value)op[key]=Number($('extra-'+id).value);
   if($('extra-export'))op.isExportSale=$('extra-export').checked;if($('extra-settled'))op.isSettled=$('extra-settled').checked;
   if($('extra-details'))op.details=$('extra-details').value;if($('extra-instrument'))op.isInstrumentNote=$('extra-instrument').checked;
   if(action==='sell'){if(op.receivedAmount>op.price)throw Error('مبلغ دریافتی نباید بیشتر از فروش باشد.');const source=(snap.leadSources||[]).find(x=>x.id===op.leadSourceId);if(source?.requiresReferrer&&!op.referrerId)throw Error('معرف را انتخاب کنید.');if(op.referrerId&&!source?.requiresReferrer)throw Error('برای معرف، روش آشنایی معرف را انتخاب کنید.');}
   if(action==='edit'){
    const targets=bowls.filter(x=>x.code===b.code||x.code===b.pairCode);
    op.editScales=Object.fromEntries(targets.map(x=>[x.code,$('scale-'+x.code).value||null]));
    op.edit={handpanScaleId:$('extra-scale')?.value||null,bowlScales:targets.map(x=>({bowlId:x.id,scaleId:$('scale-'+x.code).value||null})),eventUsers:[]};
    for(const event of snap.editableEvents||[])if($('event-'+event.id))op.edit.eventUsers.push({eventId:event.id,userId:$('event-'+event.id).value});
   }
   if(action==='photo'){OfflineNative.photo(JSON.stringify(op));return;}saveExtended(op);
  }catch(error){$('message').textContent=error.message;}
 });
}
$('new-bowl').addEventListener('click',()=>{
 if(!state.snapshot||state.locked){$('message').textContent='ابتدا دریافت اطلاعات و ورود مدیر لازم است.';return;}
 $('workspace').innerHTML=`<h2>ساخت کاسه جدید</h2><form id="create-bowl">${input('create-code','کد کاسه','text','',true)}${select('create-material','متریال',inventory().filter(m=>m.category===4).map(m=>({id:m.id,name:`${m.name} · رو ${m.topQuantity} · زیر ${m.bottomQuantity}`})))}${select('create-type','نوع کاسه',[{id:'1',name:'کاسه رو'},{id:'2',name:'کاسه زیر'}])}<label><input id="create-notes" type="checkbox">کاسه زیر نت‌دار</label>${select('create-instrument','نوع تولید',[{id:'1',name:'استاندارد'},{id:'2',name:'سفارشی'}],'1')}<button type="submit">ذخیره کاسه روی گوشی</button></form>`;
 $('create-bowl').addEventListener('submit',e=>{e.preventDefault();try{
  const code=model.normalize($('create-code').value),materialId=$('create-material').value,bowlType=Number($('create-type').value),m=inventory().find(x=>x.id===materialId);
  if(!/^[A-Z0-9][A-Z0-9-]{0,49}$/.test(code))throw Error('کد معتبر با حروف لاتین و عدد وارد کنید.');if(bowls.some(x=>x.code===code))throw Error('کد قبلاً ثبت شده است.');if(!m||(bowlType===1?m.topQuantity:m.bottomQuantity)<1)throw Error('موجودی کاسه کافی نیست.');
  saveExtended({code,action:'create',performerId:state.snapshot.ownerId,expectedVersion:'new',materialId,bowlType,hasNotes:$('create-notes').checked,instrumentType:Number($('create-instrument').value),materialVersions:materialVersions([materialId])});
 }catch(error){$('message').textContent=error.message;}});
});
$('inventory').addEventListener('click',()=>{
 if(!state.snapshot)return;
 $('workspace').innerHTML='<h2>موجودی محلی</h2><p class="muted">آخرین اطلاعات دریافتی به همراه تغییرات همین گوشی؛ وضعیت تأیید در صف مشخص است.</p>'+inventory().map(m=>`<div class="history">${escape(m.name)} · ${m.category===4?'رو '+m.topQuantity+' · زیر '+m.bottomQuantity:m.quantity}${state.locked?'':` <button data-stock="${escape(m.id)}">ورود / اصلاح موجودی</button>`}</div>`).join('')+'<div id="stock-form"></div><h2>سازها و کاسه‌ها</h2>'+[2,4,6,8,10,11,12,14,16,18,20,21,22].map(stage=>{const items=bowls.filter(b=>b.stage===stage&&(b.instrumentCode?b.type===1:true));return items.length?`<details><summary>${escape(model.labels[stage])} · ${items.length}</summary>${items.map(b=>`<button data-bowl="${escape(b.code)}">${escape(b.instrumentCode||b.code)}</button>`).join('')}</details>`:'';}).join('');
 $('workspace').onclick=e=>{if(e.target.dataset.bowl){selected=e.target.dataset.bowl;renderPassport();return;}const m=inventory().find(x=>x.id===e.target.dataset.stock);if(!m)return;
  $('stock-form').innerHTML=`<form id="adjust-stock"><h2>${escape(m.name)}</h2>${m.category===4?input('stock-top','کاسه رو','number',0,true)+input('stock-bottom','کاسه زیر','number',0,true):input('stock-quantity','تعداد','number',0,true)}<label><input id="stock-absolute" type="checkbox">جایگزینی موجودی (در غیر این صورت، افزودن)</label><button type="submit">ذخیره موجودی روی گوشی</button></form>`;
  $('adjust-stock').addEventListener('submit',event=>{event.preventDefault();try{const op={code:'@'+m.id,action:'stock',performerId:state.snapshot.ownerId,expectedVersion:'stock',materialId:m.id,materialVersions:materialVersions([m.id]),quantity:Number($('stock-quantity')?.value||0),topQuantity:Number($('stock-top')?.value||0),bottomQuantity:Number($('stock-bottom')?.value||0),setAbsolute:$('stock-absolute').checked};if([op.quantity,op.topQuantity,op.bottomQuantity].some(x=>!Number.isInteger(x)||x<0||x>1000000))throw Error('تعداد باید عدد صحیح بین صفر و یک میلیون باشد.');if(!op.setAbsolute&&op.quantity+op.topQuantity+op.bottomQuantity===0)throw Error('مقدار ورودی را وارد کنید.');saveExtended(op);}catch(error){$('message').textContent=error.message;}});
 };
});
window.refreshOffline();
