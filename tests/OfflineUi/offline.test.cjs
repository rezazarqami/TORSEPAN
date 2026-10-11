const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),{test}=require('node:test');
const {JSDOM,ResourceLoader,VirtualConsole}=require('jsdom');
const root=path.resolve(__dirname,'../../android/app/src/main/assets/offline');
class BundledAssets extends ResourceLoader{fetch(url){const name=new URL(url).pathname.split('/').pop();if(!['app.js','model.js','style.css'].includes(name))throw Error('unexpected network access '+url);return Promise.resolve(fs.readFileSync(path.join(root,name)));}}
async function fixture(){
 const errors=[],snapshot={schema:1,deviceId:'fixture',ownerId:'owner',capturedUtc:new Date().toISOString(),bowls:[{code:'855',type:1,hasNotes:true,stage:2,stageName:'در انتظار دیمپل',version:'v1',pairCode:null},{code:'196',type:2,hasNotes:false,stage:2,stageName:'در انتظار دیمپل',version:'v2',pairCode:null}],users:[{id:'actor',name:'همکار آزمایشی',roles:['Administrator']}],scales:[{id:'scale',name:'D Kurd 14',usage:63}],passports:{records:[{code:'855',design:'ساده',scale:'ثبت نشده',operations:[]},{code:'196',design:'ساده',scale:'ثبت نشده',operations:[]}]}};
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
 input('search','۸۵۵');assert.ok($('operation'));$('performerId').value='actor';$('duration').value='2';$('scaleId').value='scale';submit();
 assert.equal(state.operations.length,1);assert.match(dom.window.document.querySelector('.identity').textContent,/در انتظار شیپ/);
 $('performerId').value='actor';$('duration').value='3';dom.window.refreshOffline();assert.equal($('duration').value,'3');assert.equal($('performerId').value,'actor');submit();
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
 assert.ok($('operation'));$('performerId').value='actor';$('duration').value='2';$('scaleId').value='scale';submit();
 $('performerId').value='actor';$('duration').value='2';submit();$('performerId').value='actor';submit();$('performerId').value='actor';$('duration').value='2';submit();
 $('performerId').value='actor';$('pairCode').value='196';$('pairCode').dispatchEvent(new w.Event('change'));$('scaleId').value='scale';submit();
 $('performerId').value='actor';submit();$('performerId').value='actor';$('duration').value='2';submit();$('performerId').value='actor';submit();
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
