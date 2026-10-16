const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{test}=require('node:test');
const {JSDOM,ResourceLoader,VirtualConsole}=require('jsdom');
const root=path.resolve(__dirname,'../../android/app/src/main/assets/offline');
class BundledAssets extends ResourceLoader{fetch(url){const name=new URL(url).pathname.split('/').pop();if(!['app.js','model.js','style.css','workspace.js'].includes(name))throw Error('unexpected network access '+url);return Promise.resolve(fs.readFileSync(path.join(root,name)));}}
async function fixture(){
 const errors=[],snapshot={schema:1,deviceId:'fixture',ownerId:'owner',capturedUtc:new Date().toISOString(),bowls:[{code:'855',type:1,hasNotes:true,stage:2,stageName:'در انتظار دیمپل',version:'v1',pairCode:null},{code:'196',type:2,hasNotes:false,stage:2,stageName:'در انتظار دیمپل',version:'v2',pairCode:null}],users:[{id:'actor',name:'همکار آزمایشی',roles:['Administrator']}],scales:[{id:'scale',name:'D Kurd',usage:63}],passports:{records:[{code:'855',design:'ساده',scale:'ثبت نشده',operations:[]},{code:'196',design:'ساده',scale:'ثبت نشده',operations:[]}]}};
 snapshot.warehouses=[{id:1,name:'Economy'},{id:2,name:'Studio'},{id:3,name:'Signature'}];
 const state={snapshot,operations:[],locked:false,message:'اطلاعات آزمایشی'};
 const console=new VirtualConsole();console.on('jsdomError',e=>errors.push(e.message));
 const dom=new JSDOM(fs.readFileSync(path.join(root,'index.html'),'utf8'),{url:'https://appassets.androidplatform.net/offline/index.html',runScripts:'dangerously',resources:new BundledAssets(),virtualConsole:console,beforeParse(window){window.OfflineNative={read:()=>JSON.stringify(state),save:raw=>{const body=JSON.parse(raw),id='op-'+(state.operations.length+1);body.operationId=id;body.occurredUtc=new Date().toISOString();state.operations.push({id,body,state:'pending',message:'در انتظار ارسال'});return JSON.stringify({id});},activate:()=>{},sync:()=>{},retry:()=>{}};}});
 await new Promise(resolve=>dom.window.addEventListener('load',resolve,{once:true}));
 const $=id=>dom.window.document.getElementById(id),input=(id,value)=>{$(id).value=value;$(id).dispatchEvent(new dom.window.Event('input',{bubbles:true}));};
 const submit=()=>{$('operation').dispatchEvent(new dom.window.Event('submit',{bubbles:true,cancelable:true}));};
 return {dom,state,errors,$,input,submit};
}
test('local screen searches Persian codes and commits sequential operations; refresh preserves unfinished forms',async()=>{
 const f=await fixture();const {$,input,submit,dom,state,errors}=f;
 input('search','۸۵۵');assert.ok($('operation'));$('performerId').value='actor';$('duration').value='2';assert.equal($('scaleId'),null);submit();
 assert.equal(state.operations.length,1);assert.match(dom.window.document.querySelector('.identity').textContent,/در انتظار شیپ/);
 $('performerId').value='actor';$('duration').value='3';$('scaleId').value='scale';$('noteCount').value='9';dom.window.refreshOffline();assert.equal($('duration').value,'3');assert.equal($('performerId').value,'actor');assert.equal($('noteCount').value,'9');submit();
 assert.equal(state.operations.length,2);assert.match(dom.window.document.querySelector('.identity').textContent,/در انتظار پخت/);
 $('performerId').value='actor';submit();assert.equal(state.operations.length,3);assert.match(dom.window.document.querySelector('.identity').textContent,/در انتظار تیون/);
 assert.deepEqual(errors,[]);dom.window.close();
});
test('merged local passport includes independent bottom/top performers and escapes untrusted descriptions',async()=>{
 const {dom,state,$,input,errors}=await fixture();
 state.snapshot.passports.records[0].operations=[{action:'شیپ',bowl:'کاسه رو',actor:'<img src=x onerror=alert(1)>',description:'<script>bad()</script>',date:new Date().toISOString()}];
 state.operations=[['855','dimple'],['855','shape'],['855','bake'],['855','tune'],['196','dimple'],['196','shape'],['196','bake'],['855','glue']].map(([code,action],i)=>({id:'op'+i,state:'pending',message:'waiting',body:{code,action,scaleId:'scale',pairCode:action==='glue'?'196':null,performerId:'actor',occurredUtc:new Date().toISOString()}}));
 dom.window.refreshOffline();input('search','855');const passport=$('passport').textContent;
 assert.match(passport,/ثبت شیپ · کاسه رو 855/);assert.match(passport,/ثبت شیپ · کاسه زیر 196/);assert.equal($('passport').querySelectorAll('script,img').length,0);
 assert.deepEqual(errors,[]);dom.window.close();
});

test('new bowl can run through packaging and sale locally with stock projection and persistent photo selection metadata',async()=>{
 const f=await fixture(),{$,dom,state,errors,submit}=f,w=dom.window;
 state.snapshot.materials=[{id:'steel',name:'Steel',category:4,topQuantity:3,bottomQuantity:3,quantity:0,version:'material-v1'},{id:'triangle',name:'مثلثی',category:3,quantity:6,version:'pack-v1'}];
 state.snapshot.users.push({id:'owner',name:'مدیر',roles:['Administrator']});state.snapshot.bowls[1].stage=10;state.snapshot.bowls[1].stageName='در انتظار چسب';
 w.refreshOffline();$('new-bowl').click();$('create-code').value='NEW-001';$('create-material').value='steel';$('create-type').value='1';
 $('create-bowl').dispatchEvent(new w.Event('submit',{bubbles:true,cancelable:true}));assert.equal(state.operations[0].body.action,'create');
 assert.ok($('operation'));$('performerId').value='actor';$('duration').value='2';assert.equal($('scaleId'),null);submit();
 $('performerId').value='actor';$('duration').value='2';$('scaleId').value='scale';$('noteCount').value='9';submit();$('performerId').value='actor';submit();$('performerId').value='actor';$('duration').value='2';submit();
 $('performerId').value='actor';$('pairCode').value='196';$('pairCode').dispatchEvent(new w.Event('change'));$('scaleId').value='scale';submit();
 $('performerId').value='actor';submit();$('performerId').value='actor';$('duration').value='2';submit();$('performerId').value='actor';$('qualityWarehouseId').value='1';submit();
 let photo;w.OfflineNative.photo=raw=>photo=JSON.parse(raw);dom.window.document.querySelector('[data-extra="photo"]').click();$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(photo.code,'NEW-001');assert.equal(photo.action,'photo');
 dom.window.document.querySelector('[data-extra="packaging"]').click();$('material-triangle').checked=true;
 w.refreshOffline();assert.equal($('material-triangle').checked,true,'periodic refresh preserves an unfinished packaging form');
 $('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations.at(-1).body.action,'packaging');assert.equal(state.operations.at(-1).body.materialVersions.triangle,'pack-v1');
 dom.window.document.querySelector('[data-extra="sell"]').click();$('extra-buyer').value='خریدار';$('extra-price').value='5000000';$('extra-received').value='2000000';$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));
 assert.equal(state.operations.at(-1).body.action,'sell');assert.match(dom.window.document.querySelector('.identity').textContent,/فروخته‌شده/);
 assert.equal(w.OfflineModel.materials(state.snapshot,state.operations).find(m=>m.id==='steel').topQuantity,2);
 assert.equal(w.OfflineModel.materials(state.snapshot,state.operations).find(m=>m.id==='triangle').quantity,4);
 $('inventory').click();assert.match($('workspace').textContent,/فروخته‌شده/);assert.deepEqual(errors,[]);dom.window.close();
});

test('offline notes, design, administrative edits and stock corrections enqueue validated data without remote navigation',async()=>{
 const f=await fixture(),{$,dom,state,errors,input}=f,w=dom.window;
 state.snapshot.users.push({id:'owner',name:'مدیر',roles:['Administrator']});state.snapshot.materials=[{id:'steel',name:'Steel',category:4,topQuantity:3,bottomQuantity:3,quantity:0,version:'stock'}];state.snapshot.designs=[{id:'acid',name:'اسیدی'}];state.snapshot.bowls[0].id='bowl-id';
 w.refreshOffline();input('search','855');
 dom.window.document.querySelector('[data-extra="note"]').click();$('extra-details').value='ثبت یادداشت';$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations.at(-1).body.details,'ثبت یادداشت');
 dom.window.document.querySelector('[data-extra="design"]').click();$('extra-design').value='acid';$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.match(dom.window.document.querySelector('.identity').textContent,/اسیدی/);
 dom.window.document.querySelector('[data-extra="edit"]').click();$('scale-855').value='scale';$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations.at(-1).body.editScales['855'],'scale');
 $('inventory').click();dom.window.document.querySelector('[data-stock="steel"]').click();$('stock-top').value='7';$('stock-bottom').value='8';$('stock-absolute').checked=true;$('adjust-stock').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations.at(-1).body.action,'stock');assert.equal(w.OfflineModel.materials(state.snapshot,state.operations)[0].topQuantity,7);
 assert.deepEqual(errors,[]);dom.window.close();
});

function addWorkspace(f){const s=f.state.snapshot;s.supportsLocalEventUsers=true;s.workspace={version:'workspace-v1',sections:{warehouses:s.warehouses,users:s.users.map(u=>({id:u.id,fullName:u.name,userName:u.id,roles:u.roles,isActive:true,isDeleted:false})),roles:[],scales:s.scales.map(x=>({...x,isActive:true})),materials:s.materials||[],orders:[],documents:[],parties:[],rates:[],payments:[],events:[],bowls:[],assemblies:[],handpans:[],designTypes:[],sources:[],referrers:[],marketing:[],messages:[]}};f.dom.window.refreshOffline();}
test('management remains local across refresh, creates a party, references it in accounting, and adds a payment',async()=>{
 const f=await fixture(),{dom,state,$,errors}=f,w=dom.window;addWorkspace(f);
 dom.window.document.querySelector('[data-module="parties"]').click();dom.window.document.querySelector('[data-new]').click();$('wf-name').value='مشتری آفلاین';w.refreshOffline();assert.equal($('wf-name').value,'مشتری آفلاین');$('workspace-form').dispatchEvent(new w.Event('submit',{cancelable:true}));
 assert.equal(state.operations.length,1);const id=state.operations[0].body.workspaceData.id;assert.ok(id);assert.match($('module-content').textContent,/مشتری آفلاین/);
 dom.window.document.querySelector('[data-module="documents"]').click();dom.window.document.querySelector('[data-new]').click();$('wf-type').value='1';$('wf-title').value='فروش آزمایشی';$('wf-totalAmount').value='1000';$('wf-paidAmount').value='200';$('wf-partyId').value=id;$('workspace-form').dispatchEvent(new w.Event('submit',{cancelable:true}));
 assert.equal(state.operations[1].body.workspaceData.partyId,id);dom.window.document.querySelector('[data-payment]').click();$('wf-amount').value='300';$('small-form').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations[2].body.workspaceAction,'payment');assert.match($('module-content').textContent,/۵۰۰/);assert.deepEqual(errors,[]);dom.window.close();
});
test('offline orders retain multiple lines and finalization through reload projections',async()=>{
 const f=await fixture(),{dom,state,$,errors}=f,w=dom.window;addWorkspace(f);
 dom.window.document.querySelector('[data-module="orders"]').click();dom.window.document.querySelector('[data-new]').click();$('wf-customerName').value='سفارش آفلاین';$('wf-line-scale-0').value='scale';$('wf-line-quantity-0').value='2';$('add-line').click();$('wf-line-scale-1').value='scale';$('wf-line-quantity-1').value='1';$('workspace-form').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations[0].body.workspaceData.lines.length,2);assert.ok(state.operations[0].body.workspaceData.lines[0].lineId);dom.window.document.querySelector('[data-finalize]').click();assert.equal(state.operations[1].body.workspaceAction,'finalize');w.refreshOffline();assert.match($('module-content').textContent,/قطعی/);assert.deepEqual(errors,[]);dom.window.close();
});
test('an unsent tune can be reassigned to a colleague without altering the original durable operation',async()=>{
 const f=await fixture(),{dom,state,$,input,errors}=f,w=dom.window;state.snapshot.users.push({id:'owner',name:'مدیر',roles:['Administrator']});addWorkspace(f);state.snapshot.bowls[0].stage=8;state.operations=[{id:'local-tune',body:{action:'tune',code:'855',performerId:'actor',occurredUtc:new Date().toISOString()},state:'pending',message:''}];w.refreshOffline();input('search','855');dom.window.document.querySelector('[data-extra="edit"]').click();assert.ok($('local-event-local-tune'));$('local-event-local-tune').value='owner';$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations[1].body.localEventUsers[0].operationId,'local-tune');assert.equal(state.operations[1].body.localEventUsers[0].userId,'owner');assert.equal(state.operations[0].body.performerId,'actor');assert.deepEqual(errors,[]);dom.window.close();
});

test('offline QC requires a warehouse, preserves classification during packaging, and transfers stock between tiers',async()=>{
 const f=await fixture(),{dom,state,$,input,errors}=f,w=dom.window;state.snapshot.users.push({id:'owner',name:'مدیر',roles:['Administrator']});state.snapshot.bowls[0].id='top';state.snapshot.bowls[0].stage=14;state.snapshot.bowls[0].instrumentCode='855';state.snapshot.bowls[0].pairCode='196';state.snapshot.bowls[1].id='bottom';state.snapshot.bowls[1].stage=14;state.snapshot.bowls[1].instrumentCode='855';state.snapshot.bowls[1].pairCode='855';addWorkspace(f);w.refreshOffline();input('search','855');$('performerId').value='actor';$('operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations.length,0,'QC must select a destination');$('qualityWarehouseId').value='3';$('operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations[0].body.qualityWarehouseId,3);dom.window.document.querySelector('[data-extra="packaging"]').click();$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));dom.window.document.querySelector('[data-extra="warehouseTransfer"]').click();$('extra-warehouse').value='2';$('extra-operation').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations.at(-1).body.qualityWarehouseId,2);$('inventory').click();const tabs=$('workspace').querySelectorAll('[data-tier]');assert.equal(tabs.length,3);dom.window.document.querySelector('[data-tier="2"]').click();assert.match($('offline-tier-items').textContent,/855/);dom.window.document.querySelector('[data-tier="3"]').click();assert.match($('offline-tier-items').textContent,/خالی/);assert.deepEqual(errors,[]);dom.window.close();
});
test('offline naming manager edits a stable numeric warehouse ID without moving inventory',async()=>{
 const f=await fixture(),{dom,state,$,errors}=f,w=dom.window;addWorkspace(f);dom.window.document.querySelector('[data-module="warehouses"]').click();dom.window.document.querySelector('[data-edit="2"]').click();$('wf-name').value='Atelier';$('workspace-form').dispatchEvent(new w.Event('submit',{cancelable:true}));assert.equal(state.operations[0].body.workspaceData.id,2);assert.equal(state.operations[0].body.workspaceModule,'warehouses');assert.match($('module-content').textContent,/Atelier/);assert.deepEqual(errors,[]);dom.window.close();
});

test('note-only bottom bowls display count and side without an empty-scale placeholder',async()=>{
 const {dom,state,$,input,errors}=await fixture();
 for(const count of [2,3]){
  Object.assign(state.snapshot.bowls[1],{stage:6,stageName:'در انتظار پخت',scaleId:null,noteCount:count});
  dom.window.refreshOffline();input('search','196');
  const identity=dom.window.document.querySelector('.identity').textContent;
  assert.ok(identity.includes(count+' نت زیر'));assert.ok(!identity.includes('بدون اسکیل'));
 }
 assert.deepEqual(errors,[]);dom.window.close();
});


test('named bottom scale with unknown count never displays the instrument total as bowl notes',async()=>{
 const {dom,state,input,errors}=await fixture();
 state.snapshot.scales.push({id:'named',name:'D aegean 21',usage:63});
 Object.assign(state.snapshot.bowls[1],{stage:6,scaleId:'named',noteCount:null});
 dom.window.refreshOffline();input('search','196');
 const identity=dom.window.document.querySelector('.identity').textContent;
 assert.ok(identity.includes('D aegean 21'));assert.ok(identity.includes('تعداد نت نامشخص'));assert.ok(!identity.includes('21 نوت'));
 assert.deepEqual(errors,[]);dom.window.close();
});


test('gluing chooses only a family and displays the derived top-plus-bottom note total',async()=>{
 const {dom,state,$,input,submit,errors}=await fixture();
 Object.assign(state.snapshot.bowls[0],{stage:10,noteCount:10});Object.assign(state.snapshot.bowls[1],{stage:10,noteCount:8,hasNotes:true});
 dom.window.refreshOffline();input('search','855');$('performerId').value='actor';$('pairCode').value='196';$('pairCode').dispatchEvent(new dom.window.Event('change'));$('scaleId').value='scale';submit();
 assert.equal(state.operations[0].body.action,'glue');assert.ok(!Object.hasOwn(state.operations[0].body,'noteCount'));
 assert.match(dom.window.document.querySelector('.identity').textContent,/D Kurd 18/);assert.deepEqual(errors,[]);dom.window.close();
});
